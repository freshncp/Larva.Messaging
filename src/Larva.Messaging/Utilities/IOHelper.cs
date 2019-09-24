using System;
using System.IO;

namespace Larva.Messaging.Utilities
{
    /// <summary>
    /// IO帮助类
    /// </summary>
    public class IOHelper
    {
        /// <summary>
        /// 是否IO错误
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static bool IsIOError(Exception ex)
        {
            if (ex is IOException)
            {
                return true;
            }
            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                if (ex is IOException)
                {
                    return true;
                }
                innerEx = ex.InnerException;
            }
            return false;
        }
    }
}
