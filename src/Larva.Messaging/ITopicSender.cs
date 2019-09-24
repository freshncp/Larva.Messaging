using Larva.Messaging.Serialization;
using System;

namespace Larva.Messaging
{
    /// <summary>
    /// Topic 发送者接口
    /// </summary>
    public interface ITopicSender : IMessageSender, ISerializerOwner, IDisposable
    {
        /// <summary>
        /// 交换器
        /// </summary>
        string ExchangeName { get; }

        /// <summary>
        /// 队列数
        /// </summary>
        byte QueueCount { get; }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">消息</param>
        void SendMessage<T>(T message) where T : class;

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息</param>
        void SendMessage<T>(Envelope<T> message) where T : class;
    }
}
