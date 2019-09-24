using System;

namespace Larva.Messaging
{
    /// <summary>
    /// 消息处理器重复注册异常
    /// </summary>
    public class MessageHandlerDuplicateRegistrationException : Exception
    {
        /// <summary>
        /// 消息处理器重复注册异常
        /// </summary>
        public MessageHandlerDuplicateRegistrationException() { }

        /// <summary>
        /// 消息处理器重复注册异常
        /// </summary>
        /// <param name="messageTypeName"></param>
        /// <param name="messageHandlerType"></param>
        /// <param name="registry"></param>
        public MessageHandlerDuplicateRegistrationException(string messageTypeName, Type messageHandlerType, IMessageHandlerRegistry registry)
            : base($"MessageHandler for message type \"{messageTypeName}\" has registerd by \"{messageHandlerType.FullName}\"  in \"{registry.RegistryName}\"")
        {
            MessageTypeName = messageTypeName;
            MessageHandlerType = messageHandlerType;
            Registry = registry;
        }

        /// <summary>
        /// 消息处理器已注册异常
        /// </summary>
        /// <param name="messageTypeName"></param>
        /// <param name="messageHandlerType"></param>
        /// <param name="registry"></param>
        /// <param name="innerException"></param>
        public MessageHandlerDuplicateRegistrationException(string messageTypeName, Type messageHandlerType, IMessageHandlerRegistry registry, Exception innerException)
            : base($"MessageHandler for message type \"{messageTypeName}\" has registerd by \"{messageHandlerType.FullName}\"  in \"{registry.RegistryName}\"", innerException)
        {
            MessageTypeName = messageTypeName;
            MessageHandlerType = messageHandlerType;
            Registry = registry;
        }

        /// <summary>
        /// 注册器名
        /// </summary>
        public IMessageHandlerRegistry Registry { get; private set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public string MessageTypeName { get; private set; }

        /// <summary>
        /// 消息处理器类型
        /// </summary>
        public Type MessageHandlerType { get; private set; }
    }
}
