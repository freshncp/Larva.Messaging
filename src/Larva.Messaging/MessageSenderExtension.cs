namespace Larva.Messaging
{
    /// <summary>
    /// 消息发送者扩展类
    /// </summary>
    public static class MessageSenderExtension
    {
        /// <summary>
        /// 获取发送者类型
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public static MessageSenderType GetSenderType(this ITopicSender sender)
        {
            return MessageSenderType.TopicSender;
        }

        /// <summary>
        /// 获取发送者类型
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public static MessageSenderType GetSenderType(this IPubsubSender sender)
        {
            return MessageSenderType.PubsubSender;
        }

        /// <summary>
        /// 获取发送者类型
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public static MessageSenderType GetSenderType(this IRpcClient sender)
        {
            return MessageSenderType.RpcClient;
        }

        /// <summary>
        /// 获取发送者类型
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public static MessageSenderType GetSenderType(this IMessageSender sender)
        {
            if(sender is ITopicSender)
            {
                return MessageSenderType.TopicSender;
            }
            else if(sender is IPubsubSender)
            {
                return MessageSenderType.PubsubSender;
            }
            else if(sender is IRpcClient)
            {
                return MessageSenderType.RpcClient;
            }
            else
            {
                return MessageSenderType.Undefined;
            }
        }
    }
}
