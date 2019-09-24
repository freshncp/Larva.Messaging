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
    /// Topic 发送者
    /// </summary>
    public class TopicSender : AbstractSender, ITopicSender, IDisposable
    {
        private ILog _logger = LogManager.GetLogger(typeof(TopicSender));
        private Connection _conn;
        private int _maxRetryCount;
        private bool _confirmEnabled;
        private bool _debugEnabled;

        /// <summary>
        /// Topic 发送者
        /// </summary>
        /// <param name="conn">连接</param>
        /// <param name="exchangeName">交换器</param>
        /// <param name="queueCount">队列个数</param>
        /// <param name="serializer">序列化工具</param>
        /// <param name="maxRetryCount">最大重试次数（0：不重试）</param>
        /// <param name="confirmEnabled">启用发送接收应答</param>
        /// <param name="sourceExchangeName">源Exchange交换器</param>
        /// <param name="debugEnabled">启用调试模式</param>
        public TopicSender(Connection conn, string exchangeName
            , byte queueCount = 1, ISerializer serializer = null, int maxRetryCount = -1, bool confirmEnabled = false, string sourceExchangeName = ""
            , bool debugEnabled = false)
            : base(serializer, debugEnabled)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                throw new ArgumentNullException("exchangeName", "must not empty");
            }
            if (queueCount <= 0)
            {
                throw new ArgumentOutOfRangeException("queueCount", "must greate than 0");
            }
            _conn = conn ?? throw new ArgumentNullException("conn");
            _maxRetryCount = maxRetryCount < 0 ? 0 : maxRetryCount;
            _confirmEnabled = confirmEnabled;
            ExchangeName = exchangeName;
            QueueCount = queueCount;
            _debugEnabled = debugEnabled;

            using (var channelForConfig = _conn.CreateChannel())
            {
                var dlxQueueName = $"{ExchangeName}-dlx";
                var dlxMessageTTL = 604800000;// 1周过期
                _conn.DeclareQueue(channelForConfig, dlxQueueName, new Dictionary<string, object> {
                    { "x-message-ttl", dlxMessageTTL },
                });

                _conn.DeclareDirectExchange(channelForConfig, ExchangeName);
                for (var i = 0; i < queueCount; i++)
                {
                    var queueName = $"{ExchangeName}-{i}";
                    _conn.DeclareQueue(channelForConfig, queueName, new Dictionary<string, object> {
                        { "x-dead-letter-exchange", "" },
                        { "x-dead-letter-routing-key", dlxQueueName },
                    });
                    _conn.BindQueue(channelForConfig, queueName, ExchangeName, i.ToString());
                }

                if (!string.IsNullOrEmpty(sourceExchangeName))
                {
                    _conn.BindExchange(channelForConfig, ExchangeName, sourceExchangeName, string.Empty);
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
        /// 队列数
        /// </summary>
        public byte QueueCount { get; private set; }

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
            uint routingKeyHashCode = 0;
            if (!uint.TryParse(message.RoutingKey, out routingKeyHashCode))
            {
                routingKeyHashCode = (uint)message.RoutingKey.GetHashCode();
            }
            var routingKey = (routingKeyHashCode % QueueCount).ToString();
            var body = Serializer.Serialize(message.Body);
            var messageTypeName = MessageTypeAttribute.GetTypeName(message.Body.GetType());
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
            TriggerOnMessageSent(new MessageSentEventArgs(this.GetSenderType(), context));

            try
            {
                if (!IsRunning()) throw new ObjectDisposedException(nameof(TopicSender));
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
