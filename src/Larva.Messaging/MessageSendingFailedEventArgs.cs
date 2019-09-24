using System;

namespace Larva.Messaging
{
    /// <summary>
    /// 发送消息失败 事件参数
    /// </summary>
    public class MessageSendingFailedEventArgs : EventArgs
    {
        /// <summary>
        /// 发送消息失败 事件参数
        /// </summary>
        /// <param name="senderType">发送者类型</param>
        /// <param name="context">消息传输上下文</param>
        /// <param name="ex">异常</param>
        public MessageSendingFailedEventArgs(MessageSenderType senderType, IMessageTransportationContext context)
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

        /// <summary>
        /// 异常
        /// </summary>
        public Exception Exception
        {
            get
            {
                return Context.LastException;
            }
        }
    }
}
