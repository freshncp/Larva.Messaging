using System;
using Larva.Messaging.Serialization;
using System.Collections.Generic;

namespace Larva.Messaging
{
    /// <summary>
    /// Rpc 服务端接口
    /// </summary>
    public interface IRpcServer : IMessageReceiver, ISerializerOwner, IMessageHandlerRegistry, IDisposable
    {
        /// <summary>
        /// 交换器
        /// </summary>
        string ExchangeName { get; }

        /// <summary>
        /// 方法名列表
        /// </summary>
        IList<string> MethodNameList { get; }

        /// <summary>
        /// 接收请求
        /// </summary>
        void ReceiveRequest();
    }
}
