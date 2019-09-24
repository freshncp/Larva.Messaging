using Larva.Messaging.Serialization;
using Larva.Messaging.Utilities;
using log4net;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Larva.Messaging.RabbitMQ
{
    /// <summary>
    /// Pubsub 接收者
    /// </summary>
    public class PubsubReceiver : AbstractReceiver, IPubsubReceiver, IDisposable
    {
        private ILog _logger = LogManager.GetLogger(typeof(PubsubReceiver));
        private Connection _conn;
        private List<IModel> _channels = null;
        private List<EventingBasicConsumer> _consumers = null;
        private ushort _parallelDegree;
        private bool _debugEnabled;

        /// <summary>
        /// Pubsub 接收者
        /// </summary>
        /// <param name="conn">连接</param>
        /// <param name="queueName">队列</param>
        /// <param name="parallelDegree">并行度</param>
        /// <param name="serializer">序列化工具</param>
        /// <param name="maxRetryCount">最大重试次数（0：不重试）</param>
        /// <param name="debugEnabled">启用调试模式</param>
        public PubsubReceiver(Connection conn, string queueName
            , ushort parallelDegree = 1, ISerializer serializer = null, int maxRetryCount = 1, bool debugEnabled = false)
            : base(serializer, maxRetryCount, debugEnabled)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException("queueName", "must not empty");
            }
            _conn = conn ?? throw new ArgumentNullException("conn");
            QueueName = queueName;
            if (parallelDegree == 0)
            {
                _parallelDegree = 1;
            }
            else
            {
                _parallelDegree = parallelDegree;
            }
            _debugEnabled = debugEnabled;

            _channels = new List<IModel>();
            _consumers = new List<EventingBasicConsumer>();

            for (var i = 0; i < _parallelDegree; i++)
            {
                var channel = _conn.CreateChannel();
                channel.BasicQos(0, 1, false);
                _channels.Add(channel);
            }

            _conn.OnConnectionForced += (sender, e) =>
            {
                Dispose();
            };
        }

        /// <summary>
        /// 注册器名
        /// </summary>
        /// <returns></returns>
        protected override string GetRegistryName()
        {
            return QueueName;
        }

        /// <summary>
        /// 队列
        /// </summary>
        public string QueueName { get; private set; }

        /// <summary>
        /// 订阅指定队列
        /// </summary>
        public void Subscribe()
        {
            var exceptionList = new List<Exception>();
            foreach (var channel in _channels)
            {
                if (_consumers.Exists(e => e.Model == channel)) continue;
                var consumer = new EventingBasicConsumer(channel);
                try
                {
                    consumer.Received += (sender, e) =>
                    {
                        var currentConsumer = ((EventingBasicConsumer)sender);
                        var currentChannel = currentConsumer.Model;
                        HandleMessage(currentConsumer, e.BasicProperties, e.Exchange, QueueName, e.RoutingKey, e.Body, e.DeliveryTag, e.Redelivered, false);
                    };

                    consumer.Shutdown += (sender, e) =>
                    {
                        var currentConsumer = ((EventingBasicConsumer)sender);
                        var currentChannel = currentConsumer.Model;
                        _logger.Warn($"PubsubReceiver.Consumer.Shutdown: QueueName={QueueName}, ChannelIsOpen={currentChannel.IsOpen}, Initiator={e.Initiator}, ClassId={e.ClassId}, MethodId={e.MethodId}, ReplyCode={e.ReplyCode}, ReplyText={e.ReplyText}");
                        while (RabbitMQExceptionHelper.IsChannelError(e.ReplyCode) && !currentChannel.IsOpen)
                        {
                            Thread.Sleep(1000);
                        }
                    };

                    LockerExecuter.Execute(channel, () =>
                    {
                        try
                        {
                            RetryPolicy.Retry(() => channel.BasicConsume(QueueName, false, consumer),
                            retryCondition: ex => IOHelper.IsIOError(ex) || RabbitMQExceptionHelper.IsChannelError(ex),
                            maxRetryCount: 1,
                            retryTimeInterval: 1000);
                        }
                        catch (Exception ex)
                        {
                            var realEx = ex is TargetInvocationException ? ex.InnerException : ex;
                            _logger.Error(realEx.Message, realEx);
                            throw new TargetException(realEx.Message, realEx);
                        }
                    });
                }
                catch (Exception ex)
                {
                    exceptionList.Add(ex);
                }
                finally
                {
                    _consumers.Add(consumer);
                }
            }
            if (exceptionList.Count > 0)
            {
                throw new AggregateException(exceptionList);
            }
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            foreach (var channel in _channels)
            {
                if (channel.IsOpen)
                {
                    channel.Close(Constants.ConnectionForced, "PubsubReceiver's normal channel disposed");
                }
            }
            _consumers.Clear();
        }
    }
}
