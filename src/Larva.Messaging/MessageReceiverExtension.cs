namespace Larva.Messaging
{
    /// <summary>
    /// 消息接收者扩展类
    /// </summary>
    public static class MessageReceiverExtension
    {
        /// <summary>
        /// 获取接收者类型
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        public static MessageReceiverType GetReceiverType(this ITopicReceiver receiver)
        {
            return MessageReceiverType.TopicReceiver;
        }

        /// <summary>
        /// 获取接收者类型
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        public static MessageReceiverType GetReceiverType(this IPubsubReceiver receiver)
        {
            return MessageReceiverType.PubsubReceiver;
        }

        /// <summary>
        /// 获取接收者类型
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        public static MessageReceiverType GetReceiverType(this IRpcServer receiver)
        {
            return MessageReceiverType.RpcServer;
        }

        /// <summary>
        /// 获取接收者类型
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        public static MessageReceiverType GetReceiverType(this IMessageReceiver receiver)
        {
            if(receiver is ITopicReceiver)
            {
                return MessageReceiverType.TopicReceiver;
            }
            else if(receiver is IPubsubReceiver)
            {
                return MessageReceiverType.PubsubReceiver;
            }
            else if(receiver is IRpcServer)
            {
                return MessageReceiverType.RpcServer;
            }
            else
            {
                return MessageReceiverType.Undefined;
            }
        }
    }
}
