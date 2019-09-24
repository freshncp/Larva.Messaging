namespace Larva.Messaging
{
    /// <summary>
    /// 消息处理器接口
    /// </summary>
    public interface IMessageHandler
    {
    }

    /// <summary>
    /// 消息处理器接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageHandler<T> : IMessageHandler
        where T : class
    {
        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        void Handle(T message, IMessageTransportationContext context);
    }
}
