using Larva.Messaging.Serialization;
using Larva.Messaging.Utilities;
using log4net;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
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
        private int _maxRetryCount;
        private bool _confirmEnabled;
        private bool _disableQueuePrefix;
        private bool _debugEnabled;

        /// <summary>
        /// Rpc 客户端
        /// </summary>
        /// <param name="conn">连接</param>
        /// <param name="serializer">序列化工具</param>
        /// <param name="maxRetryCount">最大重试次数（0：不重试）</param>
        /// <param name="confirmEnabled">启用发送接收应答</param>
        /// <param name="disableQueuePrefix">禁用队列前缀</param>
        /// <param name="debugEnabled">启用调试模式</param>
        public RpcClient(Connection conn, string exchangeName = "rpc"
            , ISerializer serializer = null, int maxRetryCount = 3, bool confirmEnabled = false, bool disableQueuePrefix = false, bool debugEnabled = false)
            : base(exchangeName, serializer, debugEnabled)
        {
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
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
        /// 发送消息
        /// </summary>
        /// <param name="message">消息</param>
        public override void SendMessage<T>(Envelope<T> message)
        {
            if (message == null || message.Body == null) throw new ArgumentNullException(nameof(message));
            var queueName = _disableQueuePrefix ? message.RoutingKey : $"{ExchangeName}.{message.RoutingKey}";
            var callbackQueueName = $"{queueName}.callback";
            var body = Serializer.Serialize(message.Body);
            var messageTypeName = MessageTypeAttribute.GetTypeName(message.Body.GetType());
            var routingKey = message.RoutingKey;
            var correlationId = message.MessageId;
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
                if (!TriggerOnMessageSendingFailed(new MessageSendingFailedEventArgs(this.GetSenderType(), context)))
                {
                    throw new MessageSendFailedException(envelopedMessage, ExchangeName, realEx);
                }
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
