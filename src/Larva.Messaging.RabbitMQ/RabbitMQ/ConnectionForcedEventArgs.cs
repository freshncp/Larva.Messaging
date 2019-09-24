using System;

namespace Larva.Messaging.RabbitMQ
{
    /// <summary>
    /// 连接被强制关闭事件参数
    /// </summary>
    public class ConnectionForcedEventArgs : EventArgs
    {
        /// <summary>
        /// 连接被强制关闭事件参数
        /// </summary>
        /// <param name="reason"></param>
        public ConnectionForcedEventArgs(string reason)
        {
            Reason = reason;
        }

        /// <summary>
        /// 理由
        /// </summary>
        public string Reason { get; private set; }
    }
}
