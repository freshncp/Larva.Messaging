using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing;
using System;

namespace Larva.Messaging.RabbitMQ
{
    internal class RabbitMQExceptionHelper
    {
        /// <summary>
        /// 是否通道错误
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static bool IsChannelError(Exception ex)
        {
            var operationInterruptedEx = ex as OperationInterruptedException;
            if (operationInterruptedEx != null)
            {
                return IsChannelError(operationInterruptedEx.ShutdownReason.ReplyCode);
            }
            return false;
        }

        /// <summary>
        /// 是否通道错误
        /// </summary>
        /// <param name="replyCode"></param>
        /// <returns></returns>
        public static bool IsChannelError(ushort replyCode)
        {
            return replyCode == Constants.ChannelError
                    || replyCode == Constants.InternalError;
        }
    }
}
