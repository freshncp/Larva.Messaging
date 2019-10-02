using System;

namespace Larva.Messaging
{
    /// <summary>
    /// 消息发送者
    /// </summary>
    public interface IMessageSender
    {
        /// <summary>
        /// 交换器
        /// </summary>
        string ExchangeName { get; }

        /// <summary>
        /// 消息已发送 事件
        /// </summary>
        event EventHandler<MessageSentEventArgs> OnMessageSent;

        /// <summary>
        /// 发送消息失败 事件
        /// </summary>
        event EventHandler<MessageSendingFailedEventArgs> OnMessageSendingFailed;

        /// <summary>
        /// 发送消息成功 事件
        /// </summary>
        event EventHandler<MessageSendingSucceededEventArgs> OnMessageSendingSucceeded;

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">消息</param>
        /// <param name="routingKey">路由键</param>
        void SendMessage<T>(T message, string routingKey = "") where T : class;

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息</param>
        void SendMessage<T>(Envelope<T> message) where T : class;
    }
}
