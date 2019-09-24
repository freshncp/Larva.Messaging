using System;
using System.Linq;
using System.Reflection;

namespace Larva.Messaging
{
    /// <summary>
    /// 标记消息处理器类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MessageHandlerTypeAttribute : Attribute
    {
        /// <summary>
        /// 类型名，默认取类的带名字空间的名称
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 类别，默认表示不分类
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 获取消息处理器类型名
        /// </summary>
        /// <param name="messageHandlerType"></param>
        /// <returns></returns>
        public static string GetTypeName(Type messageHandlerType)
        {
            if (messageHandlerType.IsInterface || messageHandlerType.IsAbstract || messageHandlerType.IsGenericType) return string.Empty;
            if (!messageHandlerType.GetInterfaces().Any(m => m.IsGenericType && !m.IsGenericTypeDefinition && m.GetGenericTypeDefinition() == typeof(IMessageHandler<>))) return string.Empty;

            var messageHandlerTypeAttr = messageHandlerType.GetCustomAttribute<MessageHandlerTypeAttribute>(false);
            return messageHandlerTypeAttr != null && !string.IsNullOrWhiteSpace(messageHandlerTypeAttr.TypeName) ? messageHandlerTypeAttr.TypeName.Trim() : messageHandlerType.FullName;
        }

        /// <summary>
        /// 获取消息处理器类别
        /// </summary>
        /// <param name="messageHandlerType"></param>
        /// <returns></returns>
        public static string GetCategory(Type messageHandlerType)
        {
            if (messageHandlerType.IsInterface || messageHandlerType.IsAbstract || messageHandlerType.IsGenericType) return string.Empty;
            if(!messageHandlerType.GetInterfaces().Any(m => m.IsGenericType && !m.IsGenericTypeDefinition && m.GetGenericTypeDefinition() == typeof(IMessageHandler<>))) return string.Empty;

            var messageHandlerTypeAttr = messageHandlerType.GetCustomAttribute<MessageHandlerTypeAttribute>(false);
            return messageHandlerTypeAttr != null && !string.IsNullOrWhiteSpace(messageHandlerTypeAttr.Category) ? messageHandlerTypeAttr.Category.Trim() : string.Empty;
        }
    }
}
