using System;

namespace Larva.Messaging.Serialization
{
    /// <summary>
    /// 序列化工具
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// MIME
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        byte[] Serialize(object value);

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        object Deserialize(byte[] value, Type type);

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        T Deserialize<T>(byte[] value);

        /// <summary>
        /// 文本转字节数组
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        byte[] GetBytes(string text);

        /// <summary>
        /// 字节数组转文本
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        string GetString(byte[] value);
    }
}
