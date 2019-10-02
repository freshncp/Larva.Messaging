using Larva.Messaging;
using System;
using System.Threading;
using Tests.Messages;

namespace Tests.Consumer
{
    [MessageHandlerType(Category = "ERP.External.Commands")]
    public class MessageHandler01
        : IMessageHandler<Message01>
        , IMessageHandler<Message02>
        , IMessageHandler<Message03>
    {
        public void Handle(Message01 message, IMessageTransportationContext context)
        {
            if (message.Sequence % 2 == 1) throw new ApplicationException("业务逻辑执行报错");
            Thread.Sleep(100);
            Console.WriteLine($"Message01 sequence {message.Sequence} from queue {context.QueueName}");
        }

        public void Handle(Message02 message, IMessageTransportationContext context)
        {
            Thread.Sleep(200);
            Console.WriteLine($"Message02 from queue {context.QueueName}");
        }

        public void Handle(Message03 message, IMessageTransportationContext context)
        {
            Thread.Sleep(300);
            Console.WriteLine($"Message03 from queue {context.QueueName}");
        }
    }
}
