using System;
using System.Collections.Generic;

namespace Larva.Messaging.RabbitMQ
{
    internal class MessageHandlingTransportationContext : IMessageTransportationContext
    {
        public MessageHandlingTransportationContext(string exchangeName, string queueName, IDictionary<string, object> properties)
        {
            ExchangeName = exchangeName;
            QueueName = queueName;
            Properties = properties;
        }

        public string ExchangeName { get; private set; }

        public string QueueName { get; private set; }

        public IDictionary<string, object> Properties { get; private set; }

        public Exception LastException { get; set; }

        public int RetryCount { get; set; }
    }
}
