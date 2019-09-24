using System.Threading.Tasks;

namespace Tests.Publisher
{
    class Program
    {
        static void Main(string[] args)
        {
            Parallel.Invoke(
                () => new PublisherTester().TestTopic(),
                () => new PublisherTester().TestPubsub(),
                () => new PublisherTester().TestPubsub2(),
                () => new PublisherTester().TestRpcClient()
            );
        }
    }
}
