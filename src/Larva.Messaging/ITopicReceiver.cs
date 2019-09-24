using Larva.Messaging.Serialization;
using System;

namespace Larva.Messaging
{
    /// <summary>
    /// Topic 接收者接口
    /// </summary>
    public interface ITopicReceiver : IMessageReceiver, ISerializerOwner, IMessageHandlerRegistry, IDisposable
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
        /// 订阅指定队列
        /// </summary>
        /// <param name="queueIndex">队列索引</param>
        void Subscribe(byte queueIndex);

        /// <summary>
        /// 订阅多个队列
        /// </summary>
        /// <param name="queueIndexes">队列索引数组</param>
        void SubscribeMulti(byte[] queueIndexes);

        /// <summary>
        /// 订阅所有队列
        /// </summary>
        void SubscribeAll();
    }
}
