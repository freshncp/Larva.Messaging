using Newtonsoft.Json;
using System;
using System.Text;

namespace Larva.Messaging.Serialization
{
    /// <summary>
    /// JSON序列化工具
    /// </summary>
    public class JsonSerializer : AbstractSerializer
    {
        /// <summary>
        /// MIME
        /// </summary>
        public override string ContentType => "text/json";

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override byte[] Serialize(object value)
        {
#if DEBUG
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, Formatting.Indented));
#else
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, Formatting.None));
#endif
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override object Deserialize(byte[] value, Type type)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(value), type);
        }

        /// <summary>
        /// 文本转字节数组
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public override byte[] GetBytes(string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        /// <summary>
        /// 字节数组转文本
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override string GetString(byte[] value)
        {
            return Encoding.UTF8.GetString(value);
        }
    }
}
