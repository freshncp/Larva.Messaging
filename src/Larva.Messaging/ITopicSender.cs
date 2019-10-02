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
        /// 队列数
        /// </summary>
        byte QueueCount { get; }
    }
}
