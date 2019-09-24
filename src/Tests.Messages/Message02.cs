using Larva.Messaging;
using System;

namespace Tests.Messages
{
    [MessageType(TypeName = "Tests.Messages.2")]
    public class Message02
    {
        public Guid SaleFilialeId { get; set; }

        public Message02() { }

        public Message02(Guid saleFilialeId)
        {
            SaleFilialeId = saleFilialeId;
        }
    }
}
