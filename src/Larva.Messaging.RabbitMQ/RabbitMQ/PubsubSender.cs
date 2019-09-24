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
    /// Pubsub 发送者
    /// </summary>
    public class PubsubSender : AbstractSender, IPubsubSender, IDisposable
    {
        private ILog _logger = LogManager.GetLogger(typeof(PubsubSender));
        private Connection _conn;
        private int _maxRetryCount;
        private bool _confirmEnabled;
        private bool _atLeastMatchOneQueue;
        private bool _publishToExchange;
        private byte _publishToExchangeQueueCount;
        private bool _debugEnabled;

        /// <summary>
        /// Pubsub 发送者
        /// </summary>
        /// <param name="conn">连接</param>
        /// <param name="exchangeName">交换器</param>
        /// <param name="subscriberNames">订阅者名数组</param>
        /// <param name="serializer">序列化工具</param>
        /// <param name="maxRetryCount">最大重试次数（0：不重试）</param>
        /// <param name="confirmEnabled">启用发送接收应答</param>
        /// <param name="atLeastMatchOneQueue">至少一个匹配的队列</param>
        /// <param name="publishToExchange">发布到交换器，默认为发布到队列</param>
        /// <param name="publishToExchangeQueueCount">发布到交换器时的队列数</param>
        /// <param name="debugEnabled">启用调试模式</param>
        public PubsubSender(Connection conn, string exchangeName
            , IEnumerable<string> subscriberNames = null, ISerializer serializer = null, int maxRetryCount = -1, bool confirmEnabled = false, bool atLeastMatchOneQueue = false
            , bool publishToExchange = false, byte publishToExchangeQueueCount = 1, bool debugEnabled = false)
            : base(serializer, debugEnabled)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                throw new ArgumentNullException("exchangeName", "must not empty");
            }
            _conn = conn ?? throw new ArgumentNullException("conn");
            _maxRetryCount = maxRetryCount < 0 ? 0 : maxRetryCount;
            _confirmEnabled = confirmEnabled;
            _atLeastMatchOneQueue = confirmEnabled || atLeastMatchOneQueue;// 如果启用发送接收应答，则“至少一个匹配的队列”自动开启
            _publishToExchange = publishToExchange;
            _publishToExchangeQueueCount = publishToExchangeQueueCount;
            _debugEnabled = debugEnabled;
            ExchangeName = exchangeName;
            SubscriberNameQueueOrExchangeNameMapping = subscriberNames == null ? new Dictionary<string, string>() : subscriberNames.ToDictionary(kv => kv, kv => publishToExchange ? $"{exchangeName}.X.{kv}" : $"{exchangeName}.{kv}");

            using (var channelForConfig = _conn.CreateChannel())
            {
                _conn.DeclareFanoutExchange(channelForConfig, exchangeName);
                if (subscriberNames != null)
                {
                    foreach (var subscriberName in subscriberNames)
                    {
                        var queueName = $"{ExchangeName}.{subscriberName}";
                        if (publishToExchange)
                        {
                            _conn.UnbindQueue(channelForConfig, queueName, ExchangeName, string.Empty);
                        }
                        else
                        {
                            _conn.DeclareQueue(channelForConfig, queueName);
                            _conn.BindQueue(channelForConfig, queueName, ExchangeName, string.Empty);
                        }
                    }
                }
                channelForConfig.Close();
            }

            _conn.OnConnectionForced += (sender, e) =>
            {
                Dispose();
            };
            Run();
        }

        /// <summary>
        /// 交换器
        /// </summary>
        public string ExchangeName { get; private set; }

        /// <summary>
        /// 订阅者名->队列名或交换器名映射
        /// </summary>
        public IDictionary<string, string> SubscriberNameQueueOrExchangeNameMapping { get; private set; }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">消息</param>
        public void SendMessage<T>(T message) where T : class
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
                        .First(m => m.Name == "SendMessage"
                            && m.IsGenericMethodDefinition
                            && m.GetParameters()[0].ParameterType.Name == typeof(Envelope<>).Name)
                        .MakeGenericMethod(messageType.GetGenericArguments()[0]);
                    try
                    {
                        methodInfo.Invoke(this, new object[] { message });
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
                SendMessage(Envelope.Create(message));
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息</param>
        public void SendMessage<T>(Envelope<T> message) where T : class
        {
            if (message == null || message.Body == null) throw new ArgumentNullException("message");
            var body = Serializer.Serialize(message.Body);
            var messageTypeName = MessageTypeAttribute.GetTypeName(message.Body.GetType());
            var routingKey = string.Empty;
            if (_publishToExchange)
            {
                uint routingKeyHashCode = 0;
                if (!uint.TryParse(message.RoutingKey, out routingKeyHashCode))
                {
                    routingKeyHashCode = (uint)message.RoutingKey.GetHashCode();
                }
                routingKey = (routingKeyHashCode % _publishToExchangeQueueCount).ToString();
            }
            else
            {
                routingKey = message.RoutingKey == null ? string.Empty : message.RoutingKey;
            }
            var envelopedMessage = new EnvelopedMessage(message.MessageId,
                messageTypeName,
                message.Timestamp,
                Serializer.GetString(body),
                routingKey,
                string.Empty,
                string.Empty);

            var context = new MessageSendingTransportationContext(ExchangeName, new Dictionary<string, object> {
                { MessagePropertyConstants.MESSAGE_ID, string.IsNullOrEmpty(message.MessageId) ? Guid.NewGuid().ToString() : message.MessageId },
                { MessagePropertyConstants.MESSAGE_TYPE, messageTypeName },
                { MessagePropertyConstants.TIMESTAMP, message.Timestamp },
                { MessagePropertyConstants.CONTENT_TYPE, Serializer.ContentType },
                { MessagePropertyConstants.PAYLOAD, Serializer.GetString(body) },
                { MessagePropertyConstants.ROUTING_KEY, routingKey }
            });
            try
            {
                TriggerOnMessageSent(new MessageSentEventArgs(this.GetSenderType(), context));
                if (!IsRunning()) throw new ObjectDisposedException(nameof(PubsubSender));
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
                        if (_confirmEnabled)
                        {
                            channel.ConfirmSelect();
                        }
                        channel.BasicPublish(exchange: ExchangeName,
                                             routingKey: routingKey,
                                             mandatory: _atLeastMatchOneQueue,
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
