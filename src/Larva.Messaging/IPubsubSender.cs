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
        /// 交换器
        /// </summary>
        string ExchangeName { get; }

        /// <summary>
        /// 订阅者名->队列名或交换器名映射
        /// </summary>
        IDictionary<string, string> SubscriberNameQueueOrExchangeNameMapping { get; }

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
