using System.Threading.Tasks;

namespace Tests.Publisher
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var tester = new PublisherTester())
            {
                Parallel.Invoke(
                    () => tester.TestTopic(),
                    () => tester.TestPubsub(),
                    () => tester.TestPubsub2(),
                    () => tester.TestRpcClient()
                );
            }
        }
    }
}
