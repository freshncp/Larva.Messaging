using Larva.Messaging.Serialization;
using Larva.Messaging.Utilities;
using log4net;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Larva.Messaging.RabbitMQ
{
    /// <summary>
    /// 接收者抽象类
    /// </summary>
    public abstract class AbstractReceiver : ISerializerOwner, IMessageHandlerRegistry, IMessageReceiver
    {
        private ILog _logger = LogManager.GetLogger(typeof(AbstractReceiver));
        private Dictionary<string, Type> _messageTypes = null;
        private Dictionary<string, IMessageHandler> _messageHandlers = null;
        private bool _debugEnabled;

        /// <summary>
        /// 接收者抽象类
        /// </summary>
        /// <param name="serializer">序列化工具</param>
        /// <param name="maxRetryCount">最大重试次数（0：不重试）</param>
        /// <param name="debugEnabled">启用调试模式</param>
        protected AbstractReceiver(ISerializer serializer, int maxRetryCount, bool debugEnabled)
        {
            _messageTypes = new Dictionary<string, Type>();
            _messageHandlers = new Dictionary<string, IMessageHandler>();
            Serializer = serializer ?? new JsonSerializer();
            MaxRetryCount = maxRetryCount < 0 ? 0 : maxRetryCount;
            _debugEnabled = debugEnabled;

            OnMessageHandlingSucceeded += (sender, e) =>
            {
                if (_debugEnabled)
                {
                    _logger.Info($"Message [{e.Context.GetMessageType()}] \"{e.Context.GetPayload()}\" has handled from queue \"{e.Context.QueueName}\".");
                }
            };

            OnMessageHandlingFailed += (sender, e) =>
            {
                _logger.Error($"Message [{e.Context.GetMessageType()}] \"{e.Context.GetPayload()}\" has dropped from queue \"{e.Context.QueueName}\".", e.Context.LastException);
            };
        }

        /// <summary>
        /// 序列化工具
        /// </summary>
        public ISerializer Serializer { get; private set; }

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; private set; }

        /// <summary>
        /// 消息已接收 事件
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;

        /// <summary>
        /// 处理消息失败 事件
        /// </summary>
        public event EventHandler<MessageHandlingFailedEventArgs> OnMessageHandlingFailed;

        /// <summary>
        /// 处理消息成功 事件
        /// </summary>
        public event EventHandler<MessageHandlingSucceededEventArgs> OnMessageHandlingSucceeded;

        /// <summary>
        /// 触发 OnMessageReceived 事件
        /// </summary>
        /// <param name="args"></param>
        protected void TriggerOnMessageReceived(MessageReceivedEventArgs args)
        {
            OnMessageReceived?.Invoke(this, args);
        }

        /// <summary>
        /// 触发 OnMessageHandlingFailed 事件
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected bool TriggerOnMessageHandlingFailed(MessageHandlingFailedEventArgs args)
        {
            try
            {
                if (OnMessageHandlingFailed == null)
                {
                    throw new Exception("No event handler: OnMessageHandlingFailed");
                }
                OnMessageHandlingFailed?.Invoke(this, args);
                return true;
            }
            catch (Exception onHandleMessageFailedEx)
            {
                _logger.Error($"Message [\"{args.Context.GetMessageType()}\"] \"{args.Context.GetPayload()}\" handle message failed, and event \"{GetType().Name}.OnHandleMessageFailed\" invoke error in queue \"{args.Context.QueueName}\": {onHandleMessageFailedEx.Message}.", onHandleMessageFailedEx);
                return false;
            }
        }

        /// <summary>
        /// 触发 OnMessageHandlingSucceeded 事件
        /// </summary>
        /// <param name="args"></param>
        protected void TriggerOnMessageHandlingSucceeded(MessageHandlingSucceededEventArgs args)
        {
            try
            {
                OnMessageHandlingSucceeded?.Invoke(this, args);
            }
            catch { }
        }

        /// <summary>
        /// 获取注册器名
        /// </summary>
        /// <returns></returns>
        protected abstract string GetRegistryName();

        string IMessageHandlerRegistry.RegistryName
        {
            get
            {
                return GetRegistryName();
            }
        }

        void IMessageHandlerRegistry.RegisterMessageHandler<T>()
        {
            RegisterMessageHandler(typeof(T), string.Empty);
        }

        void IMessageHandlerRegistry.RegisterMessageHandlerByAssembly(Assembly assembly, string messageHandlerCategory)
        {
            foreach (var handlerType in assembly.GetTypes())
            {
                if (typeof(IMessageHandler).IsAssignableFrom(handlerType))
                {
                    RegisterMessageHandler(handlerType, messageHandlerCategory);
                }
            }
        }

        private void RegisterMessageHandler(Type handlerType, string filteredMessageHandlerCategory)
        {
            if (handlerType.GetTypeInfo().IsInterface || handlerType.GetTypeInfo().IsAbstract || handlerType.GetTypeInfo().IsGenericType) return;
            var currentHandlerInterfaceTypes = handlerType.GetInterfaces()
                .Where(m => m.GetTypeInfo().IsGenericType && !m.GetTypeInfo().IsGenericTypeDefinition && m.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
                .ToArray();
            var messageHandlerCategory = MessageHandlerTypeAttribute.GetCategory(handlerType);
            if (string.IsNullOrEmpty(filteredMessageHandlerCategory) || messageHandlerCategory == filteredMessageHandlerCategory)
            {
                foreach (var currentHandlerInterfaceType in currentHandlerInterfaceTypes)
                {
                    var messageType = currentHandlerInterfaceType.GenericTypeArguments[0];
                    var messageTypeName = MessageTypeAttribute.GetTypeName(messageType);
                    if (!_messageTypes.ContainsKey(messageTypeName))
                    {
                        _messageTypes.Add(messageTypeName, messageType);
                    }
                    if (!_messageHandlers.ContainsKey(messageTypeName))
                    {
                        _messageHandlers.Add(messageTypeName, (IMessageHandler)Activator.CreateInstance(handlerType));
                    }
                    else
                    {
                        var ex = new MessageHandlerDuplicateRegistrationException(messageTypeName, handlerType, this);
                        _logger.Error(ex.Message);
                        throw ex;
                    }
                }
            }
        }

        /// <summary>
        /// 处理消息队列的消息
        /// </summary>
        /// <param name="currentConsumer"></param>
        /// <param name="props"></param>
        /// <param name="exchangeName"></param>
        /// <param name="queueName"></param>
        /// <param name="routingKey"></param>
        /// <param name="body"></param>
        /// <param name="deliveryTag"></param>
        /// <param name="redelivered"></param>
        /// <param name="dropIfMessageHandlerNotRegistered">消息处理器未注册时直接丢弃</param>
        protected void HandleMessage(IBasicConsumer currentConsumer, IBasicProperties props, string exchangeName, string queueName, string routingKey, byte[] body, ulong deliveryTag, bool redelivered, bool dropIfMessageHandlerNotRegistered)
        {
            var currentChannel = currentConsumer.Model;
            var messageTypeName = props.Type;
            if (string.IsNullOrEmpty(props.Type))
            {
                // 如果未传入类型，则以队列名作为类型名
                if (queueName.StartsWith("rpc.", StringComparison.CurrentCulture))
                {
                    messageTypeName = queueName.Substring(4);
                }
                else
                {
                    messageTypeName = queueName;
                }
            }
            var context = new MessageHandlingTransportationContext(exchangeName, queueName, new Dictionary<string, object> {
                { MessagePropertyConstants.MESSAGE_ID, string.IsNullOrEmpty(props.MessageId) ? Guid.NewGuid().ToString() : props.MessageId },
                { MessagePropertyConstants.MESSAGE_TYPE, messageTypeName },
                { MessagePropertyConstants.TIMESTAMP, props.Timestamp.UnixTime == 0 ? DateTime.Now : DateTime2UnixTime.FromUnixTime(props.Timestamp.UnixTime) },
                { MessagePropertyConstants.CONTENT_TYPE, string.IsNullOrEmpty(props.ContentType) ? Serializer.ContentType : props.ContentType },
                { MessagePropertyConstants.PAYLOAD, Serializer.GetString(body) },
                { MessagePropertyConstants.ROUTING_KEY, routingKey },
                { MessagePropertyConstants.REPLY_TO, props.ReplyTo },
                { MessagePropertyConstants.CORRELATION_ID, props.CorrelationId }
            });
            var messageHandlerCategory = string.Empty;
            try
            {
                if (_messageTypes.ContainsKey(messageTypeName))
                {
                    var messageType = _messageTypes[messageTypeName];
                    object message = null;
                    try
                    {
                        message = Serializer.Deserialize(body, messageType);
                    }
                    catch
                    {
                        // 消费端无法反序列化消息，则直接丢弃
                        _logger.Error($"Message [\"{context.GetMessageType()}\"] \"{context.GetPayload()}\" deserialized fail in queue \"{queueName}\".");
                        LockerExecuter.Execute(currentChannel, () =>
                        {
                            RetryPolicy.Retry(() => currentChannel.BasicReject(deliveryTag, false),
                            retryCondition: retryEx => IOHelper.IsIOError(retryEx) || RabbitMQExceptionHelper.IsChannelError(retryEx),
                            retryTimeInterval: 1000);
                        });
                    }

                    if (message == null) return;
                    var messageHandler = GetMessageHandler(messageTypeName);
                    if (messageHandler != null)
                    {
                        messageHandlerCategory = MessageHandlerTypeAttribute.GetCategory(messageHandler.GetType());
                        TriggerOnMessageReceived(new MessageReceivedEventArgs(messageHandlerCategory, this.GetReceiverType(), context));
                        RetryPolicy.Retry(() =>
                        {
                            try
                            {
                                var handleMethodInfo = messageHandler.GetType().GetTypeInfo().GetMethod("Handle",
                                   new Type[] { messageType, typeof(IMessageTransportationContext) });
                                handleMethodInfo.Invoke(messageHandler, new object[] { message, context });
                            }
                            catch (Exception handleEx)
                            {
                                context.LastException = handleEx is TargetInvocationException ? handleEx.InnerException : handleEx;
                                throw handleEx;
                            }
                        }, cancelOnFailed: (retryCount, retryException) =>
                        {
                            if (retryCount >= MaxRetryCount)
                            {
                                return true;
                            }
                            context.LastException = retryException is TargetInvocationException ? retryException.InnerException : retryException;
                            context.RetryCount = retryCount + 1;
                            return false;
                        });

                        TriggerOnMessageHandlingSucceeded(new MessageHandlingSucceededEventArgs(messageHandlerCategory, this.GetReceiverType(), context));
                        LockerExecuter.Execute(currentChannel, () =>
                        {
                            RetryPolicy.Retry(() => currentChannel.BasicAck(deliveryTag, false),
                            retryCondition: retryEx => IOHelper.IsIOError(retryEx) || RabbitMQExceptionHelper.IsChannelError(retryEx),
                            retryTimeInterval: 1000);
                        });
                    }
                    else if (dropIfMessageHandlerNotRegistered)
                    {
                        // 消费端无对应的消息处理器，则忽略，直接响应成功
                        LockerExecuter.Execute(currentChannel, () =>
                        {
                            RetryPolicy.Retry(() => currentChannel.BasicAck(deliveryTag, false),
                            retryCondition: retryEx => IOHelper.IsIOError(retryEx) || RabbitMQExceptionHelper.IsChannelError(retryEx),
                            retryTimeInterval: 1000);
                        });
                    }
                    else
                    {
                        context.LastException = new MessageHandlerNotRegisteredException(messageTypeName);
                        if (TriggerOnMessageHandlingFailed(new MessageHandlingFailedEventArgs(messageHandlerCategory, this.GetReceiverType(), context)))
                        {
                            LockerExecuter.Execute(currentChannel, () =>
                            {
                                RetryPolicy.Retry(() => currentChannel.BasicAck(deliveryTag, false),
                                retryCondition: retryEx => IOHelper.IsIOError(retryEx) || RabbitMQExceptionHelper.IsChannelError(retryEx),
                                retryTimeInterval: 1000);
                            });
                        }
                        else
                        {
                            LockerExecuter.Execute(currentChannel, () =>
                            {
                                RetryPolicy.Retry(() => currentChannel.BasicReject(deliveryTag, false),
                                retryCondition: retryEx => IOHelper.IsIOError(retryEx) || RabbitMQExceptionHelper.IsChannelError(retryEx),
                                retryTimeInterval: 1000);
                            });
                        }
                    }
                }
                else if (dropIfMessageHandlerNotRegistered)
                {
                    // 消费端无对应的消息处理器，则忽略，直接响应成功
                    LockerExecuter.Execute(currentChannel, () =>
                    {
                        RetryPolicy.Retry(() => currentChannel.BasicAck(deliveryTag, false),
                        retryCondition: retryEx => IOHelper.IsIOError(retryEx) || RabbitMQExceptionHelper.IsChannelError(retryEx),
                        retryTimeInterval: 1000);
                    });
                }
                else
                {
                    context.LastException = new MessageHandlerNotRegisteredException(messageTypeName);
                    if (TriggerOnMessageHandlingFailed(new MessageHandlingFailedEventArgs(messageHandlerCategory, this.GetReceiverType(), context)))
                    {
                        LockerExecuter.Execute(currentChannel, () =>
                        {
                            RetryPolicy.Retry(() => currentChannel.BasicAck(deliveryTag, false),
                            retryCondition: retryEx => IOHelper.IsIOError(retryEx) || RabbitMQExceptionHelper.IsChannelError(retryEx),
                            retryTimeInterval: 1000);
                        });
                    }
                    else
                    {
                        LockerExecuter.Execute(currentChannel, () =>
                        {
                            RetryPolicy.Retry(() => currentChannel.BasicReject(deliveryTag, false),
                            retryCondition: retryEx => IOHelper.IsIOError(retryEx) || RabbitMQExceptionHelper.IsChannelError(retryEx),
                            retryTimeInterval: 1000);
                        });
                    }
                }
            }
            catch(Exception ex)
            {
                // 消费端处理其他异常
                context.LastException = ex is TargetInvocationException ? ex.InnerException : ex;
                if (TriggerOnMessageHandlingFailed(new MessageHandlingFailedEventArgs(messageHandlerCategory, this.GetReceiverType(), context)))
                {
                    LockerExecuter.Execute(currentChannel, () =>
                    {
                        RetryPolicy.Retry(() => currentChannel.BasicAck(deliveryTag, false),
                        retryCondition: retryEx => IOHelper.IsIOError(retryEx) || RabbitMQExceptionHelper.IsChannelError(retryEx),
                        retryTimeInterval: 1000);
                    });
                }
                else
                {
                    LockerExecuter.Execute(currentChannel, () =>
                    {
                        RetryPolicy.Retry(() => currentChannel.BasicReject(deliveryTag, false),
                        retryCondition: retryEx => IOHelper.IsIOError(retryEx) || RabbitMQExceptionHelper.IsChannelError(retryEx),
                        retryTimeInterval: 1000);
                    });
                }
            }
        }

        private IMessageHandler GetMessageHandler(string messageTypeName)
        {
            if (_messageHandlers.ContainsKey(messageTypeName))
            {
                return _messageHandlers[messageTypeName];
            }
            return null;
        }
    }
}
