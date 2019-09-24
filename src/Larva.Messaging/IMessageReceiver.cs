using System;

namespace Larva.Messaging
{
    /// <summary>
    /// 消息接收者
    /// </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// 消息已处理 事件
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> OnMessageReceived;

        /// <summary>
        /// 处理消息失败 事件
        /// </summary>
        event EventHandler<MessageHandlingFailedEventArgs> OnMessageHandlingFailed;

        /// <summary>
        /// 处理消息成功 事件
        /// </summary>
        event EventHandler<MessageHandlingSucceededEventArgs> OnMessageHandlingSucceeded;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        int MaxRetryCount { get; }
    }
}
