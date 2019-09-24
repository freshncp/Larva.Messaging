using System;

namespace Larva.Messaging
{
    /// <summary>
    /// 封装的消息
    /// </summary>
    [Serializable]
    public sealed class EnvelopedMessage
    {
        /// <summary>
        /// 封装的消息
        /// </summary>
        public EnvelopedMessage() { }

        /// <summary>
        /// 封装的消息
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="messageType"></param>
        /// <param name="messageTimestamp"></param>
        /// <param name="payload"></param>
        /// <param name="routingKey"></param>
        /// <param name="correlationId"></param>
        /// <param name="replyTo"></param>
        public EnvelopedMessage(string messageId, string messageType, DateTime messageTimestamp, string payload, string routingKey, string correlationId, string replyTo)
        {
            MessageId = messageId;
            MessageType = messageType;
            MessageTimestamp = messageTimestamp;
            Payload = payload;
            RoutingKey = routingKey;
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        /// <summary>
        /// 消息ID
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public string MessageType { get; private set; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        public DateTime MessageTimestamp { get; private set; }

        /// <summary>
        /// 消息重放信息（用于还原消息）
        /// </summary>
        public string Payload { get; private set; }

        /// <summary>
        /// 路由键
        /// </summary>
        public string RoutingKey { get; private set; }

        /// <summary>
        /// 关联ID
        /// </summary>
        public string CorrelationId { get; private set; }

        /// <summary>
        /// 响应队列名
        /// </summary>
        public string ReplyTo { get; private set; }
    }
}
