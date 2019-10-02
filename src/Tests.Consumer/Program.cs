using System;
using System.Threading.Tasks;

namespace Tests.Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var tester = new ConsumerTester();
            Parallel.Invoke(
                () => tester.TestTopic(),
                () => tester.TestPubsub("test.pubsub.wms"),
                () => tester.TestPubsub("test.pubsub.erp"),
                () => tester.TestPubsub2(),
                () => tester.TestRpcServer()
            );
        }
    }
}
