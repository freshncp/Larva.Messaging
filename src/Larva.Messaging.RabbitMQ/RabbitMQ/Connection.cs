using log4net;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Larva.Messaging.RabbitMQ
{
    /// <summary>
    /// 连接
    /// </summary>
    public class Connection : IDisposable
    {
        private ILog _logger = LogManager.GetLogger(typeof(Connection));
        private IConnection _connection = null;
        private bool _isDisposed = false;

        /// <summary>
        /// 连接被强制关闭事件
        /// </summary>
        public event EventHandler<ConnectionForcedEventArgs> OnConnectionForced;

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="config">配置</param>
        public Connection(ConnectionConfig config)
        {
            var factory = new ConnectionFactory();
            if (config.Uri != null)
            {
                factory.Uri = config.Uri;
            }
            else
            {
                factory.UserName = config.UserName;
                factory.Password = config.Password;
                factory.HostName = config.HostName;
                factory.Port = config.Port;
                factory.VirtualHost = config.VirtualHost;
                factory.AutomaticRecoveryEnabled = true;
                factory.TopologyRecoveryEnabled = true;
            }
            if (config.RequestedHeartbeat != 0)
            {
                factory.RequestedHeartbeat = config.RequestedHeartbeat;
            }
            if (config.SocketReadTimeout != 0)
            {
                factory.SocketReadTimeout = config.SocketReadTimeout;
            }
            if (config.SocketWriteTimeout != 0)
            {
                factory.SocketWriteTimeout = config.SocketWriteTimeout;
            }
            CreateConnection(factory);
        }

        /// <summary>
        /// 析构
        /// </summary>
        ~Connection()
        {
            if (!_isDisposed)
            {
                if (_connection != null)
                {
                    if (_connection.IsOpen)
                    {
                        _connection.Close(Constants.ConnectionForced, "Connection disposed.");
                    }
                    _connection.Dispose();
                    _connection = null;
                }
            }
        }

        /// <summary>
        /// 创建通道
        /// </summary>
        /// <returns></returns>
        public IModel CreateChannel()
        {
            var channel = _connection.CreateModel();
            channel.ModelShutdown += (sender, e) =>
            {
                if (e.ReplyCode == Constants.ReplySuccess) return;
                _logger.Warn($"ModelShutdown: Initiator={e.Initiator}, ClassId={e.ClassId}, MethodId={e.MethodId}, ReplyCode={e.ReplyCode}, ReplyText={e.ReplyText}, Cause={e.Cause}");
            };

            channel.CallbackException += (sender, e) =>
            {
                _logger.Error($"ModelCallbackException: {e.Exception.Message}", e.Exception);
                throw e.Exception;
            };

            channel.FlowControl += (sender, e) =>
            {
                _logger.Warn($"ModelFlowControl: Active={e.Active}");
            };
            return channel;
        }

        /// <summary>
        /// 创建MQ连接
        /// </summary>
        /// <param name="connectionFactory"></param>
        private void CreateConnection(IConnectionFactory connectionFactory)
        {
            var connected = false;
            var errorCount = 0;
            while (connected == false)
            {
                try
                {
                    _connection = connectionFactory.CreateConnection();
                    connected = true;
                }
                catch (BrokerUnreachableException e)
                {
                    errorCount++;
                    _logger.Error("连接异常", e);
                    Thread.Sleep(1000);
                }
                catch (ConnectFailureException e)
                {
                    errorCount++;
                    _logger.Error("连接异常", e);
                    Thread.Sleep(1000);
                }

                if (errorCount == 5)
                    throw new Exception($"connection failed after three retries. Uri:{connectionFactory.Uri} UserName:{connectionFactory.UserName}");
            }

            _connection.CallbackException += (sender, e) =>
            {
                _logger.Error("ConnectionCallbackException: {e.Exception.Message}", e.Exception);
            };
            _connection.ConnectionBlocked += (sender, e) =>
            {
                _logger.Warn($"ConnectionBlocked: Reason={e.Reason}");
            };
            _connection.ConnectionUnblocked += (sender, e) =>
            {
                _logger.Info("ConnectionUnblocked");
            };
            _connection.ConnectionShutdown += (sender, e) =>
            {
                _logger.Warn($"ConnectionShutdown: Initiator={e.Initiator}, ClassId={e.ClassId}, MethodId={e.MethodId}, ReplyCode={e.ReplyCode}, ReplyText={e.ReplyText}, Cause={e.Cause}");
                if (e.ReplyCode == Constants.ConnectionForced)
                {
                    OnConnectionForced?.Invoke(sender, new ConnectionForcedEventArgs(e.ReplyText));
                }
            };
            _connection.RecoverySucceeded += (sender, e) =>
            {
                _logger.Info("ConnectionRecoverySucceeded");
            };
            _connection.ConnectionRecoveryError += (sender, e) =>
            {
                _logger.Error("ConnectionRecoveryError: {e.Exception.Message}", e.Exception);
            };

            _logger.Info("Rabbitmq server already connected.");
        }

        /// <summary>
        /// 声明 direct 类型的交换器
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="exchangeName"></param>
        /// <param name="arguments"></param>
        public void DeclareDirectExchange(IModel channel, string exchangeName, IDictionary<string, object> arguments = null)
        {
            channel.ExchangeDeclare(exchangeName, ExchangeType.Direct, true, false, arguments);
        }

        /// <summary>
        /// 声明 fanout 类型的交换器
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="exchangeName"></param>
        /// <param name="arguments"></param>
        public void DeclareFanoutExchange(IModel channel, string exchangeName, IDictionary<string, object> arguments = null)
        {
            channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout, true, false, arguments);
        }

        /// <summary>
        /// 声明 topic 类型的交换器
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="exchangeName"></param>
        /// <param name="arguments"></param>
        public void DeclareTopicExchange(IModel channel, string exchangeName, IDictionary<string, object> arguments = null)
        {
            channel.ExchangeDeclare(exchangeName, ExchangeType.Topic, true, false, arguments);
        }

        /// <summary>
        /// 绑定队列
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="queueName"></param>
        /// <param name="exchangeName"></param>
        /// <param name="routingKey"></param>
        /// <param name="arguments"></param>
        public void BindQueue(IModel channel, string queueName, string exchangeName, string routingKey, IDictionary<string, object> arguments = null)
        {
            channel.QueueBind(queueName, exchangeName, routingKey, arguments);
        }

        /// <summary>
        /// 解绑队列
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="queueName"></param>
        /// <param name="exchangeName"></param>
        /// <param name="routingKey"></param>
        /// <param name="arguments"></param>
        public void UnbindQueue(IModel channel, string queueName, string exchangeName, string routingKey, IDictionary<string, object> arguments = null)
        {
            channel.QueueUnbind(queueName, exchangeName, routingKey, arguments);
        }

        /// <summary>
        /// 绑定交换器
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="destinationExchangeName"></param>
        /// <param name="sourceExchangeName"></param>
        /// <param name="routingKey"></param>
        /// <param name="arguments"></param>
        public void BindExchange(IModel channel, string destinationExchangeName, string sourceExchangeName, string routingKey, IDictionary<string, object> arguments = null)
        {
            channel.ExchangeBind(destinationExchangeName, sourceExchangeName, routingKey, arguments);
        }

        /// <summary>
        /// 解绑交换器
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="destinationExchangeName"></param>
        /// <param name="sourceExchangeName"></param>
        /// <param name="routingKey"></param>
        /// <param name="arguments"></param>
        public void UnbindExchange(IModel channel, string destinationExchangeName, string sourceExchangeName, string routingKey, IDictionary<string, object> arguments = null)
        {
            channel.ExchangeUnbind(destinationExchangeName, sourceExchangeName, routingKey, arguments);
        }

        /// <summary>
        /// 声明队列
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="queueName"></param>
        /// <param name="arguments"></param>
        public void DeclareQueue(IModel channel, string queueName, IDictionary<string, object> arguments = null)
        {
            channel.QueueDeclare(queueName, true, false, false, arguments);
        }

        /// <summary>
        /// 连接是否处于打开状态
        /// </summary>
        /// <returns></returns>
        public bool IsOpen()
        {
            return _connection.IsOpen;
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            if (_connection.IsOpen)
            {
                _connection.Close(Constants.ConnectionForced, "Connection disposed.");
            }
        }

        /// <summary>
        /// 释放连接
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _isDisposed = true;
            if (_connection != null)
            {
                if (_connection.IsOpen)
                {
                    _connection.Close(Constants.ConnectionForced, "Connection disposed.");
                }
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}