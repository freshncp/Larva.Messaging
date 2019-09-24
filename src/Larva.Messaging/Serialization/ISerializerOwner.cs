namespace Larva.Messaging.Serialization
{
    /// <summary>
    /// 序列化工具拥有者
    /// </summary>
    public interface ISerializerOwner
    {
        /// <summary>
        /// 序列化工具
        /// </summary>
        ISerializer Serializer { get; }
    }
}
