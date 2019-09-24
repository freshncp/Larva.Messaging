namespace Larva.Messaging
{
    /// <summary>
    /// 发送者类型
    /// </summary>
    public enum MessageSenderType : byte
    {
        /// <summary>
        /// 未定义
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Topic发送者
        /// </summary>
        TopicSender = 1,

        /// <summary>
        /// Pubsub发送者
        /// </summary>
        PubsubSender = 2,

        /// <summary>
        /// Rpc客户端
        /// </summary>
        RpcClient = 3
    }
}
