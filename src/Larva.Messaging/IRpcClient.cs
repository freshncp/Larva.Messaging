using Larva.Messaging.Serialization;
using System;

namespace Larva.Messaging
{
    /// <summary>
    /// Rpc 客户端接口
    /// </summary>
    public interface IRpcClient : IMessageSender, ISerializerOwner, IDisposable
    {
        /// <summary>
        /// 交换机名
        /// </summary>
        string ExchangeName { get; }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">消息</param>
        /// <param name="methodName">方法名</param>
        /// <param name="correlationId">关联ID</param>
        void SendRequest<T>(T message, string methodName, string correlationId) where T : class;

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="methodName">方法名</param>
        /// <param name="correlationId">关联ID</param>
        void SendRequest<T>(Envelope<T> message, string methodName, string correlationId) where T : class;
    }
}
