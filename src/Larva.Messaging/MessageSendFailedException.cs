using System;

namespace Larva.Messaging
{
    /// <summary>
    /// 消息发送失败异常
    /// </summary>
    public class MessageSendFailedException : Exception
    {
        /// <summary>
        /// 消息发送失败异常
        /// </summary>
        public MessageSendFailedException() { }

        /// <summary>
        /// 消息发送失败异常
        /// </summary>
        /// <param name="envelopedMessage"></param>
        /// <param name="exchangeName"></param>
        public MessageSendFailedException(EnvelopedMessage envelopedMessage, string exchangeName)
            : base($"Message \"{envelopedMessage.MessageType}\" send to \"{exchangeName}\" failed.")
        {
            EnvelopedMessage = envelopedMessage;
            ExchangeName = exchangeName;
        }

        /// <summary>
        /// 消息发送失败异常
        /// </summary>
        /// <param name="envelopedMessage"></param>
        /// <param name="exchangeName"></param>
        /// <param name="innerException"></param>
        public MessageSendFailedException(EnvelopedMessage envelopedMessage, string exchangeName, Exception innerException)
            : base($"Message \"{envelopedMessage.MessageType}\" send to \"{exchangeName}\" failed.", innerException)
        {
            ExchangeName = exchangeName;
            EnvelopedMessage = envelopedMessage;
        }

        /// <summary>
        /// 交换器名
        /// </summary>
        public string ExchangeName { get; private set; }

        /// <summary>
        /// 消息
        /// </summary>
        public EnvelopedMessage EnvelopedMessage { get; private set; }
    }
}
