using System;
using System.Reflection;

namespace Larva.Messaging
{
    /// <summary>
    /// 标记消息类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MessageTypeAttribute : Attribute
    {
        /// <summary>
        /// 类型名，默认取类的带名字空间的名称
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 获取消息类型名
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public static string GetTypeName(Type messageType)
        {
            if (messageType.IsInterface || messageType.IsAbstract || messageType.IsGenericType) return string.Empty;

            var messageTypeAttr = messageType.GetCustomAttribute<MessageTypeAttribute>(false);
            return messageTypeAttr != null && !string.IsNullOrWhiteSpace(messageTypeAttr.TypeName) ? messageTypeAttr.TypeName.Trim() : messageType.FullName;
        }
    }
}
