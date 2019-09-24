using System;

namespace Larva.Messaging
{
    /// <summary>
    /// 发送消息成功 事件参数
    /// </summary>
    public class MessageSendingSucceededEventArgs : EventArgs
    {
        /// <summary>
        /// 发送消息成功 事件参数
        /// </summary>
        /// <param name="senderType">发送者类型</param>
        /// <param name="context">消息传输上下文</param>
        public MessageSendingSucceededEventArgs(MessageSenderType senderType, IMessageTransportationContext context)
        {
            SenderType = senderType;
            Context = context;
        }

        /// <summary>
        /// 发送者类型
        /// </summary>
        public MessageSenderType SenderType { get; private set; }

        /// <summary>
        /// 消息传输上下文
        /// </summary>
        public IMessageTransportationContext Context { get; private set; }
    }
}
