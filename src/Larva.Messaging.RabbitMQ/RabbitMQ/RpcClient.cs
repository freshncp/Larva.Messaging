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
    /// Rpc 客户端
    /// </summary>
    public class RpcClient : AbstractSender, IRpcClient, IDisposable
    {
        private ILog _logger = LogManager.GetLogger(typeof(RpcClient));
        private Connection _conn;
        private string _exchangeName;
        private int _maxRetryCount;
        private bool _confirmEnabled;
        private bool _disableQueuePrefix;
        private bool _debugEnabled;

        /// <summary>
        /// Rpc 客户端
        /// </summary>
        /// <param name="conn">连接</param>
        /// <param name="exchangeName">交换器名</param>
        /// <param name="serializer">序列化工具</param>
        /// <param name="maxRetryCount">最大重试次数（0：不重试）</param>
        /// <param name="confirmEnabled">启用发送接收应答</param>
        /// <param name="disableQueuePrefix">禁用队列前缀</param>
        /// <param name="debugEnabled">启用调试模式</param>
        public RpcClient(Connection conn, string exchangeName = "rpc"
            , ISerializer serializer = null, int maxRetryCount = 1, bool confirmEnabled = false, bool disableQueuePrefix = false, bool debugEnabled = false)
            : base(serializer, debugEnabled)
        {
            _conn = conn ?? throw new ArgumentNullException("conn");
            _exchangeName = exchangeName ?? string.Empty;
            _maxRetryCount = maxRetryCount < 0 ? 0 : maxRetryCount;
            _confirmEnabled = confirmEnabled;
            _disableQueuePrefix = disableQueuePrefix;
            _debugEnabled = debugEnabled;

            _conn.OnConnectionForced += (sender, e) =>
            {
                Dispose();
            };
            Run();
        }

        /// <summary>
        /// 交换机名
        /// </summary>
        public string ExchangeName
        {
            get { return _exchangeName; }
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">消息</param>
        /// <param name="methodName">方法名</param>
        /// <param name="correlationId">关联ID</param>
        public void SendRequest<T>(T message, string methodName, string correlationId) where T : class
        {
            if (message == null) throw new ArgumentNullException("message");
            Type messageType = message.GetType();
            if (typeof(Envelope).IsAssignableFrom(messageType))
            {
                if (messageType.IsGenericType
                    && !messageType.IsGenericTypeDefinition
                    && typeof(Envelope<>) == messageType.GetGenericTypeDefinition())
                {
                    var methodInfo = GetType().GetMethods()
                        .First(m => m.Name == "SendRequest"
                            && m.IsGenericMethodDefinition
                            && m.GetParameters()[0].ParameterType.Name == typeof(Envelope<>).Name)
                        .MakeGenericMethod(messageType.GetGenericArguments()[0]);
                    try
                    {
                        methodInfo.Invoke(this, new object[] { message, methodName, correlationId });
                    }
                    catch (Exception ex)
                    {
                        var realEx = ex is TargetInvocationException ? ex.InnerException : ex;
                        throw new TargetInvocationException(realEx.Message, realEx);
                    }
                }
            }
            else
            {
                SendRequest(Envelope.Create(message), methodName, correlationId);
            }
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="methodName">方法名</param>
        /// <param name="correlationId">关联ID</param>
        public void SendRequest<T>(Envelope<T> message, string methodName, string correlationId) where T : class
        {
            if (message == null || message.Body == null) throw new ArgumentNullException("message");
            var queueName = _disableQueuePrefix ? methodName : $"rpc.{methodName}";
            var callbackQueueName = $"{queueName}.callback";
            var body = Serializer.Serialize(message.Body);
            var messageTypeName = MessageTypeAttribute.GetTypeName(message.Body.GetType());
            var routingKey = string.IsNullOrEmpty(ExchangeName) ? queueName : methodName;
            var envelopedMessage = new EnvelopedMessage(message.MessageId,
                messageTypeName,
                message.Timestamp,
                Serializer.GetString(body),
                routingKey,
                correlationId,
                callbackQueueName);

            var context = new MessageSendingTransportationContext(ExchangeName, new Dictionary<string, object> {
                { MessagePropertyConstants.MESSAGE_ID, string.IsNullOrEmpty(message.MessageId) ? Guid.NewGuid().ToString() : message.MessageId },
                { MessagePropertyConstants.MESSAGE_TYPE, messageTypeName },
                { MessagePropertyConstants.TIMESTAMP, message.Timestamp },
                { MessagePropertyConstants.CONTENT_TYPE, Serializer.ContentType },
                { MessagePropertyConstants.PAYLOAD, Serializer.GetString(body) },
                { MessagePropertyConstants.ROUTING_KEY, routingKey },
                { MessagePropertyConstants.REPLY_TO, callbackQueueName },
                { MessagePropertyConstants.CORRELATION_ID, correlationId }
            });
            TriggerOnMessageSent(new MessageSentEventArgs(this.GetSenderType(), context));
            try
            {
                if (!IsRunning()) throw new ObjectDisposedException(nameof(RpcClient));
                IncreaseRetryingMessageCount();

                RetryPolicy.Retry(() =>
                {
                    using (var channel = _conn.CreateChannel())
                    {
                        var properties = channel.CreateBasicProperties();
                        properties.Persistent = true;
                        properties.DeliveryMode = 2;
                        properties.ContentType = Serializer.ContentType;
                        properties.MessageId = message.MessageId;
                        properties.Type = messageTypeName;
                        properties.Timestamp = new AmqpTimestamp(DateTime2UnixTime.ToUnixTime(message.Timestamp));
                        properties.ReplyTo = callbackQueueName;
                        properties.CorrelationId = correlationId;
                        if (_confirmEnabled)
                        {
                            channel.ConfirmSelect();
                        }
                        channel.BasicPublish(exchange: ExchangeName,
                                             routingKey: routingKey,
                                             mandatory: true,
                                             basicProperties: properties,
                                             body: body);
                        if (_confirmEnabled && !channel.WaitForConfirms())
                        {
                            throw new Exception("Wait for confirm after sending message failed");
                        }
                    }
                },
                cancelOnFailed: (retryCount, retryException) =>
                {
                    return false;
                },
                retryCondition: ex => IOHelper.IsIOError(ex) || RabbitMQExceptionHelper.IsChannelError(ex),
                retryTimeInterval: 1000,
                maxRetryCount: _maxRetryCount);
                TriggerOnMessageSendingSucceeded(new MessageSendingSucceededEventArgs(this.GetSenderType(), context));
            }
            catch (Exception ex)
            {
                var realEx = ex is TargetInvocationException ? ex.InnerException : ex;
                context.LastException = realEx;
                TriggerOnMessageSendingFailed(new MessageSendingFailedEventArgs(this.GetSenderType(), context));
                throw new MessageSendFailedException(envelopedMessage, ExchangeName, realEx);
            }
            finally
            {
                DecreaseRetryingMessageCount();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Stop();
        }
    }
}
