using Larva.Messaging;
using System;

namespace Tests.Messages
{
    [MessageType(TypeName = "Tests.Messages.3")]
    public class Message03
    {
        public Guid SaleFilialeId { get; set; }

        public Message03() { }

        public Message03(Guid saleFilialeId)
        {
            SaleFilialeId = saleFilialeId;
        }
    }
}
