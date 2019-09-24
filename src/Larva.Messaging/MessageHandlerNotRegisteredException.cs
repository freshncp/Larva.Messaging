using System;

namespace Larva.Messaging
{
    /// <summary>
    /// 消息处理器未注册异常
    /// </summary>
    public class MessageHandlerNotRegisteredException : Exception
    {
        /// <summary>
        /// 消息处理器未注册异常
        /// </summary>
        public MessageHandlerNotRegisteredException() { }

        /// <summary>
        /// 消息处理器未注册异常
        /// </summary>
        /// <param name="messageTypeName"></param>
        public MessageHandlerNotRegisteredException(string messageTypeName)
            : base($"No messageHandler registered for message type \"{messageTypeName}\"")
        {
            MessageTypeName = messageTypeName;
        }

        /// <summary>
        /// 消息处理器未注册异常
        /// </summary>
        /// <param name="messageTypeName"></param>
        /// <param name="innerException"></param>
        public MessageHandlerNotRegisteredException(string messageTypeName, Exception innerException)
            : base($"No messageHandler registered for message type \"{messageTypeName}\"", innerException)
        {
            MessageTypeName = messageTypeName;
        }

        /// <summary>
        /// 消息类型
        /// </summary>
        public string MessageTypeName { get; private set; }
    }
}
