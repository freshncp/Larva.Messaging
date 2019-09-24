using System;

namespace Larva.Messaging
{
    /// <summary>
    /// 消息已处理 事件参数
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 消息已处理 事件参数
        /// </summary>
        /// <param name="messageHandlerCategory">消息处理器分类</param>
        /// <param name="receiverType">接收者类型</param>
        /// <param name="context">消息传输上下文</param>
        public MessageReceivedEventArgs(string messageHandlerCategory, MessageReceiverType receiverType, IMessageTransportationContext context)
        {
            MessageHandlerCategory = messageHandlerCategory;
            ReceiverType = receiverType;
            Context = context;
        }

        /// <summary>
        /// 消息处理器分类
        /// </summary>
        public string MessageHandlerCategory { get; private set; }

        /// <summary>
        /// 接收者类型
        /// </summary>
        public MessageReceiverType ReceiverType { get; private set; }

        /// <summary>
        /// 消息传输上下文
        /// </summary>
        public IMessageTransportationContext Context { get; private set; }
    }
}
