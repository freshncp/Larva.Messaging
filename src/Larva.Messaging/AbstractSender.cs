using Larva.Messaging.Serialization;
using log4net;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Larva.Messaging
{
    /// <summary>
    /// 发送者抽象类
    /// </summary>
    public abstract class AbstractSender : ISerializerOwner, IMessageSender
    {
        private ILog _logger = LogManager.GetLogger(typeof(AbstractSender));
        private long _runningState = 0;
        private long _retryingMessageCount;
        private bool _debugEnabled;

        /// <summary>
        /// 发送者抽象类
        /// </summary>
        /// <param name="exchangeName">交换器</param>
        /// <param name="serializer">序列化工具</param>
        /// <param name="debugEnabled">启用调试模式</param>
        protected AbstractSender(string exchangeName, ISerializer serializer, bool debugEnabled)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                throw new ArgumentNullException(nameof(exchangeName), "must not empty");
            }
            ExchangeName = exchangeName;
            Serializer = serializer ?? new JsonSerializer();
            _debugEnabled = debugEnabled;
            OnMessageSendingSucceeded += (sender, e) =>
            {
                if (_debugEnabled)
                {
                    _logger.Info($"Message [{e.Context.GetMessageType()}] \"{e.Context.GetPayload()}\" send to \"\", routing key=\"{e.Context.GetRoutingKey()}\", reply to=\"{e.Context.GetReplyTo()}\", correlationId=\"{e.Context.GetCorrelationId()}\" success.");
                }
            };
            OnMessageSendingFailed += (sender, e) =>
            {
                _logger.Error($"Message [{e.Context.GetMessageType()}] \"{e.Context.GetPayload()}\" send to \"\", routing key=\"{e.Context.GetRoutingKey()}\", reply to=\"{e.Context.GetReplyTo()}\", correlationId=\"{e.Context.GetCorrelationId()}\" failed.", e.Context.LastException);
            };
        }

        /// <summary>
        /// 交换器
        /// </summary>
        public string ExchangeName { get; private set; }

        /// <summary>
        /// 序列化工具
        /// </summary>
        public ISerializer Serializer { get; private set; }

        /// <summary>
        /// 消息已发送 事件
        /// </summary>
        public event EventHandler<MessageSentEventArgs> OnMessageSent;

        /// <summary>
        /// 发送消息失败 事件
        /// </summary>
        public event EventHandler<MessageSendingFailedEventArgs> OnMessageSendingFailed;

        /// <summary>
        /// 发送消息成功 事件
        /// </summary>
        public event EventHandler<MessageSendingSucceededEventArgs> OnMessageSendingSucceeded;

        /// <summary>
        /// 触发 OnMessageSent 事件
        /// </summary>
        /// <param name="args"></param>
        protected void TriggerOnMessageSent(MessageSentEventArgs args)
        {
            OnMessageSent?.Invoke(this, args);
        }

        /// <summary>
        /// 触发 OnMessageSendingFailed 事件
        /// </summary>
        /// <param name="args"></param>
        protected bool TriggerOnMessageSendingFailed(MessageSendingFailedEventArgs args)
        {
            try
            {
                if (OnMessageSendingFailed == null)
                {
                    throw new Exception("No event handler: OnMessageSendingFailed");
                }
                OnMessageSendingFailed?.Invoke(this, args);
                return true;
            }
            catch (Exception onSendMessageFailedEx)
            {
                _logger.Error($"Message [\"{args.Context.GetMessageType()}\"] \"{args.Context.GetPayload()}\" send message failed, and event \"{GetType().Name}.OnMessageSendingFailed\" invoke error in exchange \"{args.Context.ExchangeName}\", routingKey={args.Context.GetRoutingKey()}: {onSendMessageFailedEx.Message}.", onSendMessageFailedEx);
                return false;
            }
        }

        /// <summary>
        /// 触发 OnMessageSendingSucceeded 事件
        /// </summary>
        /// <param name="args"></param>
        protected void TriggerOnMessageSendingSucceeded(MessageSendingSucceededEventArgs args)
        {
            try
            {
                OnMessageSendingSucceeded?.Invoke(this, args);
            }
            catch { }
        }

        /// <summary>
        /// 增加重试的消息数
        /// </summary>
        protected void IncreaseRetryingMessageCount()
        {
            Interlocked.Increment(ref _retryingMessageCount);
        }

        /// <summary>
        /// 减少重试的消息数
        /// </summary>
        protected void DecreaseRetryingMessageCount()
        {
            Interlocked.Decrement(ref _retryingMessageCount);
        }

        /// <summary>
        /// 启动
        /// </summary>
        protected void Run()
        {
            Interlocked.CompareExchange(ref _runningState, 1, 0);
        }

        /// <summary>
        /// 停止
        /// </summary>
        protected void Stop()
        {
            if (IsRunning())
            {
                BeginStop();
                while (Interlocked.Read(ref _retryingMessageCount) > 0)
                {
                    Thread.Sleep(1000);
                }
                EndStop();
            }
        }

        /// <summary>
        /// 开始停止
        /// </summary>
        private void BeginStop()
        {
            Interlocked.CompareExchange(ref _runningState, 2, 1);
        }

        /// <summary>
        /// 停止完成
        /// </summary>
        private void EndStop()
        {
            Interlocked.CompareExchange(ref _runningState, 0, 2);
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        /// <returns></returns>
        public bool IsRunning()
        {
            return Interlocked.Read(ref _runningState) == 1;
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">消息</param>
		/// <param name="routingKey">路由键</param>
        public void SendMessage<T>(T message, string routingKey = "") where T : class
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Type messageType = message.GetType();
            if (typeof(Envelope).IsAssignableFrom(messageType))
            {
                if (messageType.GetTypeInfo().IsGenericType
                    && !messageType.GetTypeInfo().IsGenericTypeDefinition
                    && typeof(Envelope<>) == messageType.GetGenericTypeDefinition())
                {
                    var methodInfo = GetType().GetMethods()
                        .First(m => m.Name == "SendMessage"
                            && m.IsGenericMethodDefinition
                            && m.GetParameters()[0].ParameterType.Name == typeof(Envelope<>).Name)
                        .MakeGenericMethod(messageType.GetGenericArguments()[0]);
                    try
                    {
                        methodInfo.Invoke(this, new object[] { message });
                    }
                    catch (Exception ex)
                    {
                        var realEx = ex is TargetInvocationException ? ex.InnerException : ex;
                        throw new TargetInvocationException(realEx.Message, realEx);
                    }
                }
            }
            else
            {
                SendMessage(Envelope.Create(message, routingKey: routingKey));
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息</param>
        public abstract void SendMessage<T>(Envelope<T> message) where T : class;
    }
}
