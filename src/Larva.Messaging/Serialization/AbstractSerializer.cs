using System;

namespace Larva.Messaging.Serialization
{
    /// <summary>
    /// 序列化抽象类
    /// </summary>
    public abstract class AbstractSerializer : ISerializer
    {
        /// <summary>
        /// MIME
        /// </summary>
        public abstract string ContentType { get; }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract byte[] Serialize(object value);

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public abstract object Deserialize(byte[] value, Type type);

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public T Deserialize<T>(byte[] value)
        {
            return (T)Deserialize(value, typeof(T));
        }

        /// <summary>
        /// 文本转字节数组
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public abstract byte[] GetBytes(string text);

        /// <summary>
        /// 字节数组转文本
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract string GetString(byte[] value);
    }
}
