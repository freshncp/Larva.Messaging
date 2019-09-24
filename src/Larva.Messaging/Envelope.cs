using System;

namespace Larva.Messaging
{
    /// <summary>
    /// 消息封装
    /// </summary>
    public abstract class Envelope
    {
        /// <summary>
        /// 封装消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="body">消息体</param>
        /// <param name="routingKey">路由Key</param>
        /// <returns></returns>
        public static Envelope<T> Create<T>(T body, string routingKey = "") where T : class
        {
            return new Envelope<T>(body, Guid.NewGuid().ToString(), DateTime.Now) { RoutingKey = routingKey };
        }
    }

    /// <summary>
    /// 消息封装
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Envelope<T> : Envelope
        where T : class
    {
        /// <summary>
        /// 消息封装
        /// </summary>
        /// <param name="body">消息体</param>
        /// <param name="messageId">消息ID</param>
        /// <param name="timestamp">消息时间戳</param>
        public Envelope(T body, string messageId, DateTime timestamp)
        {
            Body = body;
            MessageId = messageId;
            Timestamp = timestamp;
        }

        /// <summary>
        /// 消息体
        /// </summary>
        public T Body { get; private set; }

        /// <summary>
        /// 消息ID
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// 路由Key
        /// </summary>
        public string RoutingKey { get; set; }
    }
}
