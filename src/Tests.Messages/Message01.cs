using Larva.Messaging;
using System;

namespace Tests.Messages
{
    [MessageType(TypeName = "Tests.Messages.1")]
    public class Message01
    {
        public Guid HostingFilialeId { get; set; }

        public Message01() { }

        public int Sequence { get; set; }

        public Message01(Guid hostingFilialeId)
        {
            HostingFilialeId = hostingFilialeId;
        }
    }
}
