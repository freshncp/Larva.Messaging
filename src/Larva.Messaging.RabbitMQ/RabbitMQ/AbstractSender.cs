using Larva.Messaging.Serialization;
using log4net;
using System;
using System.Threading;

namespace Larva.Messaging.RabbitMQ
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
        /// <param name="serializer">序列化工具</param>
        /// <param name="debugEnabled">启用调试模式</param>
        protected AbstractSender(ISerializer serializer, bool debugEnabled)
        {
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
        protected void TriggerOnMessageSendingFailed(MessageSendingFailedEventArgs args)
        {
            try
            {
                OnMessageSendingFailed?.Invoke(this, args);
            }
            catch { }
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
    }
}
