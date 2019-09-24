using System.Reflection;

namespace Larva.Messaging
{
    /// <summary>
    /// 消息处理器的注册器
    /// </summary>
    public interface IMessageHandlerRegistry
    {
        /// <summary>
        /// 注册器名
        /// </summary>
        string RegistryName { get; }

        /// <summary>
        /// 注册消息处理器（一个消息对应一个消息处理器）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void RegisterMessageHandler<T>() where T : class, IMessageHandler;

        /// <summary>
        /// 按程序集注册消息处理器（一个消息对应一个消息处理器）
        /// </summary>
        /// <param name="assembly">程序集</param>
        /// <param name="messageHandlerCategory">消息处理器类别</param>
        void RegisterMessageHandlerByAssembly(Assembly assembly, string messageHandlerCategory = "");
    }
}
