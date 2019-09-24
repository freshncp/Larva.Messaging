using System;
using System.Threading.Tasks;

namespace Tests.Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            Parallel.Invoke(
                () => new ConsumerTester().TestTopic(),
                () => new ConsumerTester().TestPubsub("test.pubsub.wms"),
                () => new ConsumerTester().TestPubsub("test.pubsub.erp"),
                () => new ConsumerTester().TestRpcServer()
            );
        }
    }
}
