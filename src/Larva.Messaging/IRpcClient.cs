using Larva.Messaging.Serialization;
using System;

namespace Larva.Messaging
{
    /// <summary>
    /// Rpc 客户端接口
    /// </summary>
    public interface IRpcClient : IMessageSender, ISerializerOwner, IDisposable
    {
    }
}
