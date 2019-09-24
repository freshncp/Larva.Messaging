using Larva.Messaging.Serialization;
using Larva.Messaging.Utilities;
using log4net;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Larva.Messaging.RabbitMQ
{
    /// <summary>
    /// Topic 接收者
    /// </summary>
    public class TopicReceiver : AbstractReceiver, ITopicReceiver, IDisposable
    {
        private ILog _logger = LogManager.GetLogger(typeof(TopicReceiver));
        private Connection _conn;
        private ConcurrentDictionary<byte, IModel> _channels = null;
        private ConcurrentDictionary<byte, EventingBasicConsumer> _consumers = null;
        private bool _debugEnabled;
        private bool _isDisposing = false;

        /// <summary>
        /// Topic 接收者
        /// </summary>
        /// <param name="conn">连接</param>
        /// <param name="exchangeName">交换器</param>
        /// <param name="queueCount">队列个数</param>
        /// <param name="serializer">序列化工具</param>
        /// <param name="maxRetryCount">最大重试次数（0：不重试）</param>
        /// <param name="debugEnabled">启用调试模式</param>
        public TopicReceiver(Connection conn, string exchangeName
            , byte queueCount = 1, ISerializer serializer = null, int maxRetryCount = 1, bool debugEnabled = false)
            : base(serializer, maxRetryCount, debugEnabled)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                throw new ArgumentNullException("exchangeName", "must not empty");
            }
            if (queueCount <= 0)
            {
                throw new ArgumentOutOfRangeException("queueCount", "must greate than 0");
            }
            _conn = conn ?? throw new ArgumentNullException("conn");
            _consumers = new ConcurrentDictionary<byte, EventingBasicConsumer>();
            _channels = new ConcurrentDictionary<byte, IModel>();
            ExchangeName = exchangeName;
            QueueCount = queueCount;
            _debugEnabled = debugEnabled;

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
            return ExchangeName;
        }

        /// <summary>
        /// 交换器
        /// </summary>
        public string ExchangeName { get; private set; }

        /// <summary>
        /// 队列数
        /// </summary>
        public byte QueueCount { get; private set; }

        /// <summary>
        /// 订阅指定队列
        /// </summary>
        /// <param name="queueIndex">队列索引</param>
        public void Subscribe(byte queueIndex)
        {
            if (_isDisposing) throw new ObjectDisposedException(nameof(TopicReceiver));
            if (queueIndex >= QueueCount)
            {
                throw new ArgumentOutOfRangeException("queueIndex", $"must less than {QueueCount}");
            }
            var dlxQueueName = $"{ExchangeName}-dlx";
            string queueName = $"{ExchangeName}-{queueIndex}";
            using (var channelForConfig = _conn.CreateChannel())
            {
                _conn.DeclareQueue(channelForConfig, queueName, new Dictionary<string, object> {
                    { "x-dead-letter-exchange", "" },
                    { "x-dead-letter-routing-key", dlxQueueName },
                });
                channelForConfig.Close();
            }

            _consumers.TryGetValue(queueIndex, out EventingBasicConsumer consumer);
            if (consumer != null) return;

            _channels.TryGetValue(queueIndex, out IModel channel);
            if (channel == null)
            {
                channel = _conn.CreateChannel();
                channel.BasicQos(0, 1, false);
            }
            consumer = new EventingBasicConsumer(channel);

            try
            {
                consumer.Received += (sender, e) =>
                {
                    var currentConsumer = ((EventingBasicConsumer)sender);
                    var currentChannel = currentConsumer.Model;
                    while (!_channels.Any(w => w.Value == currentChannel))
                    {
                        Thread.Sleep(1000);
                    }
                    var currentQueueIndex = _channels.First(w => w.Value == currentChannel).Key;
                    var currentQueueName = $"{ExchangeName}-{currentQueueIndex}";
                    HandleMessage(currentConsumer, e.BasicProperties, e.Exchange, currentQueueName, e.RoutingKey, e.Body, e.DeliveryTag, e.Redelivered, true);
                };

                consumer.Shutdown += (sender, e) =>
                {
                    var currentConsumer = ((EventingBasicConsumer)sender);
                    var currentChannel = currentConsumer.Model;
                    while (!_channels.Any(w => w.Value == currentChannel))
                    {
                        Thread.Sleep(1000);
                    }
                    var currentQueueIndex = _channels.First(w => w.Value == currentChannel).Key;
                    var currentQueueName = $"{ExchangeName}-{currentQueueIndex}";
                    _logger.Warn($"TopicReceiver.Consumer.Shutdown: ExchangeName={ExchangeName}, QueueName={currentQueueName}, ChannelIsOpen={currentChannel.IsOpen}, Initiator={e.Initiator}, ClassId={e.ClassId}, MethodId={e.MethodId}, ReplyCode={e.ReplyCode}, ReplyText={e.ReplyText}");
                    while (RabbitMQExceptionHelper.IsChannelError(e.ReplyCode) && !currentChannel.IsOpen)
                    {
                        Thread.Sleep(1000);
                    }
                };

                LockerExecuter.Execute(channel, () =>
                {
                    try
                    {
                        RetryPolicy.Retry(() => channel.BasicConsume(queueName, false, consumer),
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
            finally
            {
                _channels.TryAdd(queueIndex, channel);
                _consumers.TryAdd(queueIndex, consumer);
            }
        }

        /// <summary>
        /// 订阅多个队列
        /// </summary>
        /// <param name="queueIndexes">队列索引数组</param>
        public void SubscribeMulti(byte[] queueIndexes)
        {
            if (queueIndexes != null)
            {
                foreach (var queueIndex in queueIndexes)
                {
                    Subscribe(queueIndex);
                }
            }
        }

        /// <summary>
        /// 订阅所有队列
        /// </summary>
        public void SubscribeAll()
        {
            for (byte i = 0; i < QueueCount; i++)
            {
                Subscribe(i);
            }
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            _isDisposing = true;
            GC.SuppressFinalize(this);
            foreach (var channel in _channels.Values)
            {
                if (channel.IsOpen)
                {
                    channel.Close(Constants.ConnectionForced, "TopicReceiver's normal channel disposed");
                }
            }
            _channels.Clear();
            _consumers.Clear();
        }
    }
}
