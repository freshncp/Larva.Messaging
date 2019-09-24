namespace Larva.Messaging
{
    /// <summary>
    /// 消息属性常量
    /// </summary>
    public class MessagePropertyConstants
    {
        /// <summary>
        /// 消息ID（string）
        /// </summary>
        public const string MESSAGE_ID = "MessageId";

        /// <summary>
        /// 消息类型（string）
        /// </summary>
        public const string MESSAGE_TYPE = "MessageType";

        /// <summary>
        /// 消息时间戳（DateTime）
        /// </summary>
        public const string TIMESTAMP = "Timestamp";

        /// <summary>
        /// 消息MIME内容类型（string）
        /// </summary>
        public const string CONTENT_TYPE = "ContentType";

        /// <summary>
        /// 消息重放信息（string）
        /// </summary>
        public const string PAYLOAD = "Payload";

        /// <summary>
        /// 路由Key（byte）
        /// </summary>
        public const string ROUTING_KEY = "RoutingKey";

        /// <summary>
        /// 关联ID，用于RPC调用（string）
        /// </summary>
        public const string CORRELATION_ID = "CorrelationId";

        /// <summary>
        /// 响应队列，用于RPC调用（string）
        /// </summary>
        public const string REPLY_TO = "ReplyTo";
    }
}
