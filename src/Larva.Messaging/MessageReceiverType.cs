namespace Larva.Messaging
{
    /// <summary>
    /// 接受者类型
    /// </summary>
    public enum MessageReceiverType : byte
    {
        /// <summary>
        /// 未定义
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Topic接收者
        /// </summary>
        TopicReceiver = 1,

        /// <summary>
        /// Pubsub接收者
        /// </summary>
        PubsubReceiver = 2,

        /// <summary>
        /// Rpc服务端
        /// </summary>
        RpcServer = 3
    }
}
