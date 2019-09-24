using System;
using System.Collections.Generic;

namespace Larva.Messaging
{
    /// <summary>
    /// 消息传输上下文
    /// </summary>
    public interface IMessageTransportationContext
    {
        /// <summary>
        /// 交换器名
        /// </summary>
        string ExchangeName { get; }

        /// <summary>
        /// 队列名
        /// </summary>
        string QueueName { get; }

        /// <summary>
        /// 属性集合
        /// </summary>
        IDictionary<string, object> Properties { get; }

        /// <summary>
        /// 上次的异常信息
        /// </summary>
        Exception LastException { get; }

        /// <summary>
        /// 重试次数
        /// </summary>
        int RetryCount { get; }
    }    
}
