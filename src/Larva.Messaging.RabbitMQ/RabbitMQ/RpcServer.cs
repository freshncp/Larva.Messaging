using System;
using System.Collections.Concurrent;
using Larva.Messaging.Serialization;
using Larva.Messaging.Utilities;
using log4net;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection;

namespace Larva.Messaging.RabbitMQ
{
    /// <summary>
    /// Rpc 服务端
    /// </summary>
    public class RpcServer : AbstractReceiver, IRpcServer, IDisposable
    {
        private ILog _logger = LogManager.GetLogger(typeof(RpcServer));
        private Connection _conn;
        private Dictionary<string, List<IModel>> _channels = null;

        private ConcurrentDictionary<string, List<EventingBasicConsumer>> _consumers = null;
        private ushort _parallelDegree;
        private bool _disableQueuePrefix;
        private bool _debugEnabled;

        /// <summary>
        /// Rpc 服务端
        /// </summary>
        /// <param name="conn">连接</param>
        /// <param name="methodNames">方法名列表</param>
        /// <param name="exchangeName">交换器名</param>
        /// <param name="parallelDegree">并行度</param>
        /// <param name="serializer">序列化工具</param>
        /// <param name="maxRetryCount">最大重试次数（0：不重试）</param>
        /// <param name="disableQueuePrefix">禁用队列前缀</param>
        /// <param name="debugEnabled">启用调试模式</param>
        public RpcServer(Connection conn, IEnumerable<string> methodNames, string exchangeName = "rpc"
            , ushort parallelDegree = 4, ISerializer serializer = null, int maxRetryCount = 1, bool disableQueuePrefix = false, bool debugEnabled = false)
            : base(serializer, maxRetryCount, debugEnabled)
        {
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
            if (methodNames == null || !methodNames.Any())
            {
                throw new ArgumentNullException(nameof(methodNames));
            }
            if (string.IsNullOrEmpty(exchangeName))
            {
                throw new ArgumentNullException(nameof(exchangeName), "must not empty");
            }
            MethodNameList = new List<string>(methodNames).AsReadOnly();
            ExchangeName = exchangeName;
            _consumers = new ConcurrentDictionary<string, List<EventingBasicConsumer>>();
            if (parallelDegree == 0)
            {
                _parallelDegree = 4;
            }
            else
            {
                _parallelDegree = parallelDegree;
            }
            _disableQueuePrefix = disableQueuePrefix;
            _debugEnabled = debugEnabled;

            using (var channelForConfig = _conn.CreateChannel())
            {
                if (!string.IsNullOrEmpty(exchangeName))
                {
                    _conn.DeclareDirectExchange(channelForConfig, exchangeName);
                }

                foreach (var methodName in MethodNameList)
                {
                    var queueName = _disableQueuePrefix ? methodName : $"{exchangeName}.{methodName}";
                    var callbackQueueName = $"{queueName}.callback";
                    var dlxQueueName = $"{queueName}-dlx";
                    var dlxMessageTTL = 604800000;// 1周过期
                    _conn.DeclareQueue(channelForConfig, dlxQueueName, new Dictionary<string, object> {
                        { "x-message-ttl", dlxMessageTTL },
                    });

                    _conn.DeclareQueue(channelForConfig, queueName, new Dictionary<string, object> {
                        { "x-dead-letter-exchange", "" },
                        { "x-dead-letter-routing-key", dlxQueueName },
                    });
                    if (!string.IsNullOrEmpty(exchangeName))
                    {
                        _conn.BindQueue(channelForConfig, queueName, exchangeName, methodName);
                    }

                    _conn.DeclareQueue(channelForConfig, callbackQueueName);
                }

                channelForConfig.Close();
            }

            _channels = new Dictionary<string, List<IModel>>(MethodNameList.Count);
            foreach (var methodName in MethodNameList)
            {
                var queueName = _disableQueuePrefix ? methodName : $"{exchangeName}.{methodName}";
                var channelList = new List<IModel>();
                for (var i = 0; i < _parallelDegree; i++)
                {
                    var channel = _conn.CreateChannel();
                    channel.BasicQos(0, 1, false);
                    channelList.Add(channel);
                }
                _channels.Add(methodName, channelList);
            }

            _conn.OnConnectionForced += (sender, e) =>
            {
                Dispose();
            };
        }

        /// <summary>
        /// 交换器
        /// </summary>
        public string ExchangeName { get; private set; }

        /// <summary>
        /// 方法名列表
        /// </summary>
        public IList<string> MethodNameList { get; private set; }

        /// <summary>
        /// 注册器名
        /// </summary>
        /// <returns></returns>
        protected override string GetRegistryName()
        {
            return typeof(RpcServer).Name;
        }

        /// <summary>
        /// 接收请求
        /// </summary>
        public void ReceiveRequest()
        {
            var exceptionList = new List<Exception>();
            foreach (var methodName in MethodNameList)
            {
                var queueName = _disableQueuePrefix ? methodName : $"{ExchangeName}.{methodName}";
                var callbackQueueName = $"{queueName}.callback";
                var channelList = _channels[methodName];
                _consumers.TryGetValue(queueName, out List<EventingBasicConsumer> consumers);
                if (consumers == null)
                {
                    consumers = new List<EventingBasicConsumer>();
                }
                foreach (var channel in channelList)
                {
                    if (consumers.Exists(e => e.Model == channel)) continue;
                    var consumer = new EventingBasicConsumer(channel);
                    try
                    {
                        consumer.Received += (sender, e) =>
                        {
                            var currentConsumer = ((EventingBasicConsumer)sender);
                            var currentChannel = currentConsumer.Model;
                            while (!_channels.Any(w => w.Value.Contains(currentChannel)))
                            {
                                Thread.Sleep(1000);
                            }
                            var currentMethodName = _channels.First(w => w.Value.Contains(currentChannel)).Key;
                            var currentQueueName = _disableQueuePrefix ? currentMethodName : $"{ExchangeName}.{currentMethodName}";
                            HandleMessage(currentConsumer, e.BasicProperties, e.Exchange, currentQueueName, e.RoutingKey, e.Body, e.DeliveryTag, e.Redelivered, false);
                        };

                        consumer.Shutdown += (sender, e) =>
                        {
                            var currentConsumer = ((EventingBasicConsumer)sender);
                            var currentChannel = currentConsumer.Model;
                            while (!_channels.Any(w => w.Value.Contains(currentChannel)))
                            {
                                Thread.Sleep(1000);
                            }
                            var currentMethodName = _channels.First(w => w.Value.Contains(currentChannel)).Key;
                            var currentQueueName = _disableQueuePrefix ? currentMethodName : $"{ExchangeName}.{currentMethodName}";
                            _logger.Warn($"RpcServer.Consumer.Shutdown: ExchangeName={ExchangeName}, QueueName={currentQueueName}, ChannelIsOpen={currentChannel.IsOpen}, Initiator={e.Initiator}, ClassId={e.ClassId}, MethodId={e.MethodId}, ReplyCode={e.ReplyCode}, ReplyText={e.ReplyText}");
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
                    catch (Exception ex)
                    {
                        exceptionList.Add(ex);
                    }
                    finally
                    {
                        consumers.Add(consumer);
                    }
                }
                _consumers.TryAdd(queueName, consumers);
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
            foreach (var channel in _channels.Values.SelectMany(s => s).ToArray())
            {
                if (channel.IsOpen)
                {
                    channel.Close(Constants.ConnectionForced, "RpcServer's normal channel disposed");
                }
            }
            _consumers.Clear();
        }
    }
}
