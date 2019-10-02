using Larva.Messaging.Serialization;
using System;
using System.Collections.Generic;

namespace Larva.Messaging
{
    /// <summary>
    /// Pubsub 发送者接口
    /// </summary>
    public interface IPubsubSender : IMessageSender, ISerializerOwner, IDisposable
    {
        /// <summary>
        /// 是否发布到交换机（默认为到队列）
        /// </summary>
        bool PublishToExchange { get; }

        /// <summary>
        /// 发布到交换机的队列数
        /// </summary>
        byte PublishToExchangeQueueCount { get; }

        /// <summary>
        /// 订阅者名->队列名或交换器名映射
        /// </summary>
        IDictionary<string, string> SubscriberNameQueueOrExchangeNameMapping { get; }
    }
}
