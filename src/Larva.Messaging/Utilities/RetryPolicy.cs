using System;
using System.Reflection;
using System.Threading;

namespace Larva.Messaging.Utilities
{
    /// <summary>
    /// 重试策略
    /// </summary>
    public class RetryPolicy
    {
        /// <summary>
        /// 重试
        /// </summary>
        /// <param name="action">重试的方法</param>
        /// <param name="cancelOnFailed">失败的时候是否取消（默认不取消）</param>
        /// <param name="retryCondition">重试条件</param>
        /// <param name="maxRetryCount">最大重试次数</param>
        /// <param name="retryTimeInterval">重试时间间隔（单位：毫秒）</param>
        /// <returns></returns>
        public static void Retry(Action action, Func<int, Exception, bool> cancelOnFailed = null, Predicate<Exception> retryCondition = null, int maxRetryCount = -1, int retryTimeInterval = 1000)
        {
            int retryCount = 0;
            bool cancel = false;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                var lastEx = ex is TargetInvocationException ? ex.InnerException : ex;
                if(cancelOnFailed != null)
                {
                    cancel = cancelOnFailed(retryCount, lastEx);
                }
                while (true)
                {
                    if (maxRetryCount == 0
                        || (maxRetryCount > 0 && retryCount >= maxRetryCount)
                        || (retryCondition != null && !retryCondition(lastEx))
                        || cancel)
                    {
                        throw new TargetInvocationException(lastEx.Message, lastEx);
                    }
                    Thread.Sleep(retryTimeInterval);
                    retryCount++;
                    try
                    {
                        action();
                        break;
                    }
                    catch (Exception ex2)
                    {
                        lastEx = ex2 is TargetInvocationException ? ex2.InnerException : ex2;
                        if (cancelOnFailed != null)
                        {
                            cancel = cancelOnFailed(retryCount, lastEx);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 重试
        /// </summary>
        /// <typeparam name="TResult">重试的方法返回类型</typeparam>
        /// <param name="action">重试的方法</param>
        /// <param name="cancelOnFailed">失败的时候是否取消（默认不取消）</param>
        /// <param name="retryCondition">重试条件</param>
        /// <param name="maxRetryCount">最大重试次数</param>
        /// <param name="retryTimeInterval">重试时间间隔（单位：毫秒）</param>
        /// <returns></returns>
        public static TResult Retry<TResult>(Func<TResult> action, Func<int, Exception, bool> cancelOnFailed = null, Predicate<Exception> retryCondition = null, int maxRetryCount = -1, int retryTimeInterval = 1000)
        {
            int retryCount = 0;
            bool cancel = false;
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                var lastEx = ex is TargetInvocationException ? ex.InnerException : ex;
                if (cancelOnFailed != null)
                {
                    cancel = cancelOnFailed(retryCount, lastEx);
                }
                while (true)
                {
                    if (maxRetryCount == 0
                        || (maxRetryCount > 0 && retryCount >= maxRetryCount)
                        || (retryCondition != null && !retryCondition(lastEx))
                        || cancel)
                    {
                        throw new TargetInvocationException(lastEx.Message, lastEx);
                    }
                    Thread.Sleep(retryTimeInterval);
                    retryCount++;
                    try
                    {
                        return action();
                    }
                    catch (Exception ex2)
                    {
                        lastEx = ex2 is TargetInvocationException ? ex2.InnerException : ex2;
                        if (cancelOnFailed != null)
                        {
                            cancel = cancelOnFailed(retryCount, lastEx);
                        }
                    }
                }
            }
        }
    }
}
