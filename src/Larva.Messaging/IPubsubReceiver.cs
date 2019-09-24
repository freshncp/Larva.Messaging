using Larva.Messaging.Serialization;
using System;

namespace Larva.Messaging
{
    /// <summary>
    /// Pubsub 接收者接口
    /// </summary>
    public interface IPubsubReceiver : IMessageReceiver, ISerializerOwner, IMessageHandlerRegistry, IDisposable
    {
        /// <summary>
        /// 队列
        /// </summary>
        string QueueName { get; }

        /// <summary>
        /// 订阅队列
        /// </summary>
        void Subscribe();
    }
}
