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
    /// 消息重发器（用于手动重试消息）
    /// </summary>
    public class MessageRedelivery
    {
        private ILog _logger = LogManager.GetLogger(typeof(MessageRedelivery));
        private Connection _conn;
        private bool _debugEnabled;

        /// <summary>
        /// 消息重发器（用于手动重试消息）
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="serializer"></param>
        /// <param name="debugEnabled">启用调试模式</param>
        public MessageRedelivery(Connection conn
            , ISerializer serializer = null, bool debugEnabled = false)
        {
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
            Serializer = serializer ?? new JsonSerializer();
            _debugEnabled = debugEnabled;
        }

        /// <summary>
        /// 序列化工具
        /// </summary>
        public ISerializer Serializer { get; private set; }

        /// <summary>
        /// 重发到队列
        /// </summary>
        /// <param name="queueName">队列名</param>
        /// <param name="messageId">消息ID</param>
        /// <param name="messageTypeName">消息类型名</param>
        /// <param name="messageTimestamp">消息时间戳</param>
        /// <param name="messageBody">消息体</param>
        /// <param name="correlationId">关联ID</param>
        /// <param name="replyTo">响应队列名</param>
        public void RedeliverToQueue(string queueName, string messageId, string messageTypeName, DateTime messageTimestamp, string messageBody, string correlationId = "", string replyTo = "")
        {
            if (string.IsNullOrEmpty(queueName)) throw new ArgumentNullException("queueName", "must not empty");
            if (string.IsNullOrEmpty(messageId)) throw new ArgumentNullException("messageId", "must not empty");
            if (string.IsNullOrEmpty(messageTypeName)) throw new ArgumentNullException("messageTypeName", "must not empty");
            if (messageTimestamp == DateTime.MinValue) throw new ArgumentNullException("messageTimestamp", "must not empty");
            if (string.IsNullOrEmpty(messageBody)) throw new ArgumentNullException("messageBody", "must not empty");

            var body = Serializer.GetBytes(messageBody);
            var routingKey = queueName;
            var envelopedMessage = new EnvelopedMessage(messageId,
                messageTypeName,
                messageTimestamp,
                messageBody,
                routingKey,
                correlationId,
                replyTo);
            try
            {
                var context = new MessageSendingTransportationContext(string.Empty, new Dictionary<string, object> {
                    { MessagePropertyConstants.MESSAGE_ID, string.IsNullOrEmpty(messageId) ? Guid.NewGuid().ToString() : messageId },
                    { MessagePropertyConstants.MESSAGE_TYPE, messageTypeName },
                    { MessagePropertyConstants.TIMESTAMP, messageTimestamp },
                    { MessagePropertyConstants.CONTENT_TYPE, Serializer.ContentType },
                    { MessagePropertyConstants.PAYLOAD, Serializer.GetString(body) },
                    { MessagePropertyConstants.ROUTING_KEY, routingKey },
                    { MessagePropertyConstants.REPLY_TO, replyTo },
                    { MessagePropertyConstants.CORRELATION_ID, correlationId }
                });

                RetryPolicy.Retry(() =>
                {
                    using (var channel = _conn.CreateChannel())
                    {
                        var properties = channel.CreateBasicProperties();
                        properties.Persistent = true;
                        properties.DeliveryMode = 2;
                        properties.ContentType = Serializer.ContentType;
                        properties.MessageId = messageId;
                        properties.Type = messageTypeName;
                        properties.Timestamp = new AmqpTimestamp(DateTime2UnixTime.ToUnixTime(messageTimestamp));
                        if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(replyTo))
                        {
                            properties.CorrelationId = correlationId;
                            properties.ReplyTo = replyTo;
                        }

                        channel.BasicPublish(exchange: string.Empty,
                                                routingKey: routingKey,
                                                mandatory: true,
                                                basicProperties: properties,
                                                body: body);
                        channel.Close();
                    }
                },
                cancelOnFailed: (retryCount, retryException) =>
                {
                    if (retryCount == 0)
                    {
                        _logger.Error($"Message [{messageTypeName}] \"{Serializer.GetString(body)}\" send to queue \"{queueName}\" failed.", retryException);
                    }
                    return false;
                },
                retryCondition: ex => IOHelper.IsIOError(ex) || RabbitMQExceptionHelper.IsChannelError(ex),
                maxRetryCount: 1,
                retryTimeInterval: 1000);
                if (_debugEnabled)
                {
                    _logger.Info($"Message [{messageTypeName}] \"{Serializer.GetString(body)}\" send to queue \"{queueName}\" successful.");
                }
            }
            catch (Exception ex)
            {
                var realEx = ex is TargetInvocationException ? ex.InnerException : ex;
                _logger.Error($"Message [{ messageTypeName}] \"{Serializer.GetString(body)}\" send to queue \"{queueName}\" failed.", realEx);
                throw new MessageSendFailedException(envelopedMessage, string.Empty, realEx);
            }
        }

        /// <summary>
        /// 重发到交换器
        /// </summary>
        /// <param name="exchangeName">交换器名</param>
        /// <param name="messageId">消息ID</param>
        /// <param name="messageTypeName">消息类型名</param>
        /// <param name="messageTimestamp">消息时间戳</param>
        /// <param name="messageBody">消息体</param>
        /// <param name="routingKey">路由键</param>
        /// <param name="correlationId">关联ID</param>
        /// <param name="replyTo">响应队列名</param>
        public void RedeliverToExchange(string exchangeName, string messageId, string messageTypeName, DateTime messageTimestamp, string messageBody, string routingKey, string correlationId = "", string replyTo = "")
        {
            if (string.IsNullOrEmpty(messageId)) throw new ArgumentNullException("messageId", "must not empty");
            if (string.IsNullOrEmpty(messageTypeName)) throw new ArgumentNullException("messageTypeName", "must not empty");
            if (messageTimestamp == DateTime.MinValue) throw new ArgumentNullException("messageTimestamp", "must not empty");
            if (string.IsNullOrEmpty(messageBody)) throw new ArgumentNullException("messageBody", "must not empty");

            var body = Serializer.GetBytes(messageBody);
            var envelopedMessage = new EnvelopedMessage(messageId,
                messageTypeName,
                messageTimestamp,
                messageBody,
                routingKey,
                correlationId,
                replyTo);
            try
            {
                RetryPolicy.Retry(() =>
                {
                    using (var channel = _conn.CreateChannel())
                    {
                        var properties = channel.CreateBasicProperties();
                        properties.Persistent = true;
                        properties.DeliveryMode = 2;
                        properties.ContentType = Serializer.ContentType;
                        properties.MessageId = messageId;
                        properties.Type = messageTypeName;
                        properties.Timestamp = new AmqpTimestamp(DateTime2UnixTime.ToUnixTime(messageTimestamp));
                        if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(replyTo))
                        {
                            properties.CorrelationId = correlationId;
                            properties.ReplyTo = replyTo;
                        }

                        channel.BasicPublish(exchange: exchangeName ?? string.Empty,
                                                routingKey: routingKey ?? string.Empty,
                                                mandatory: true,
                                                basicProperties: properties,
                                                body: body);
                        channel.Close();
                    }
                },
                cancelOnFailed: (retryCount, retryException) =>
                {
                    if (retryCount == 0)
                    {
                        _logger.Error($"Message [{messageTypeName}] \"{Serializer.GetString(body)}\" send to exchange \"{exchangeName}\", routing key={routingKey} failed.", retryException);
                    }
                    return false;
                },
                retryCondition: ex => IOHelper.IsIOError(ex) || RabbitMQExceptionHelper.IsChannelError(ex),
                maxRetryCount: 1,
                retryTimeInterval: 1000);
                _logger.Info($"Message [{messageTypeName}] \"{Serializer.GetString(body)}\" send to exchange \"{exchangeName}\", routing key={routingKey} successful.");
            }
            catch (Exception ex)
            {
                var realEx = ex is TargetInvocationException ? ex.InnerException : ex;
                _logger.Error($"Message [{ messageTypeName}] \"{Serializer.GetString(body)}\" send to exchange \"{exchangeName}\", routing key={routingKey} failed.", realEx);
                throw new MessageSendFailedException(envelopedMessage, exchangeName, realEx);
            }
        }
    }
}
