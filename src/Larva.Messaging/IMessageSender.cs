using System;

namespace Larva.Messaging
{
    /// <summary>
    /// 消息发送者
    /// </summary>
    public interface IMessageSender
    {
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
    }
}
