using System;

namespace Larva.Messaging.RabbitMQ
{
    /// <summary>
    /// 连接配置
    /// </summary>
    public class ConnectionConfig
    {
        /// <summary>
        /// 连接配置
        /// </summary>
        public ConnectionConfig() { }

        /// <summary>
        /// 连接配置
        /// </summary>
        /// <param name="uri">URI地址，格式：amqp://user:pass@hostName:port/vhost</param>
        public ConnectionConfig(Uri uri)
        {
            Uri = uri;
        }

        /// <summary>
        /// 连接配置
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="virtualHost"></param>
        public ConnectionConfig(string hostName, int port, string userName, string password, string virtualHost)
        {
            HostName = hostName;
            Port = port;
            UserName = userName;
            Password = password;
            VirtualHost = virtualHost;
        }

        /// <summary>
        /// URI地址，格式：amqp://user:pass@hostName:port/vhost
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// 主机名
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 虚拟主机
        /// </summary>
        public string VirtualHost { get; set; }

        /// <summary>
        /// Socket读取超时（毫秒）
        /// </summary>
        public int SocketReadTimeout { get; set; }

        /// <summary>
        /// Socket写入超时（毫秒）
        /// </summary>
        public int SocketWriteTimeout { get; set; }

        /// <summary>
        /// 心跳包超时（秒）
        /// </summary>
        public ushort RequestedHeartbeat { get; set; }
    }
}
