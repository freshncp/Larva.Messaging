using System;

namespace Larva.Messaging.Utilities
{
    /// <summary>
    /// 锁执行器
    /// </summary>
    public class LockerExecuter
    {
        /// <summary>
        /// 执行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="locker"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static T Execute<T>(object locker, Func<T> func)
        {
            lock (locker)
            {
                return func();
            }
        }
        
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="locker"></param>
        /// <param name="func"></param>
        public static void Execute(object locker, Action func)
        {
            lock (locker)
            {
                func();
            }
        }
    }
}
