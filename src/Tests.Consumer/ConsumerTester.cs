using Larva.Messaging;
using Larva.Messaging.RabbitMQ;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Consumer
{
    public class ConsumerTester
    {
        private Connection _conn;

        public ConsumerTester()
        {
            _conn = new Connection(new ConnectionConfig(new Uri("amqp://test:123456@localhost:5672/test")));
        }

        public void TestTopic()
        {
            var exchangeName = "test.topic";
            byte queueCount = 4;

            ITopicReceiver receiver = new TopicReceiver(_conn, exchangeName, queueCount, debugEnabled: true);
            receiver.RegisterMessageHandlerByAssembly(typeof(MessageHandler01).Assembly);
            receiver.OnMessageHandlingFailed += (sender, args) =>
            {
                //TODO:持久化消息处理失败的信息，以备后续之后重试
                Console.WriteLine(args.Context.LastException.Message + "\r\n\t" + args.Context.LastException.StackTrace);
            };
            var receiveMessageDict = new ConcurrentDictionary<string, Tuple<bool, IMessageTransportationContext>>();
            receiver.OnMessageReceived += (sender, e) =>
            {
                receiveMessageDict.TryAdd(e.Context.GetMessageId(), new Tuple<bool, IMessageTransportationContext>(false, e.Context));
            };
            receiver.OnMessageHandlingSucceeded += (sender, e) =>
            {
                receiveMessageDict.AddOrUpdate(e.Context.GetMessageId(), new Tuple<bool, IMessageTransportationContext>(true, e.Context), (key, originVal) => new Tuple<bool, IMessageTransportationContext>(true, originVal.Item2));
            };

            Task.Run(() =>
            {
                var timeout = 100;
                var executeSeconds = 0;
                while (receiveMessageDict.Count == 0 || receiveMessageDict.Values.Any(w => !w.Item1 && w.Item2.LastException == null))
                {
                    if (executeSeconds > timeout)
                    {
                        break;
                    }
                    Thread.Sleep(1000);
                    executeSeconds++;
                }
                var failList = receiveMessageDict.Values.Where(w => !w.Item1).ToList();
            });
            receiver.SubscribeAll();
        }

        public void TestPubsub(string queueName)
        {
            IPubsubReceiver receiver = new PubsubReceiver(_conn, queueName, parallelDegree: 100, debugEnabled: true);
            receiver.RegisterMessageHandlerByAssembly(typeof(MessageHandler01).Assembly);
            receiver.OnMessageHandlingFailed += (sender, args) =>
            {
                //TODO:持久化消息处理失败的信息，以备后续之后重试
            };
            var receiveMessageDict = new ConcurrentDictionary<string, Tuple<bool, IMessageTransportationContext>>();
            receiver.OnMessageReceived += (sender, e) =>
            {
                receiveMessageDict.TryAdd(e.Context.GetMessageId(), new Tuple<bool, IMessageTransportationContext>(false, e.Context));
            };
            receiver.OnMessageHandlingSucceeded += (sender, e) =>
            {
                receiveMessageDict.AddOrUpdate(e.Context.GetMessageId(), new Tuple<bool, IMessageTransportationContext>(true, e.Context), (key, originVal) => new Tuple<bool, IMessageTransportationContext>(true, originVal.Item2));
            };

            Task.Run(() =>
            {
                var timeout = 100;
                var executeSeconds = 0;
                while (receiveMessageDict.Count == 0 || receiveMessageDict.Values.Any(w => !w.Item1 && w.Item2.LastException == null))
                {
                    if (executeSeconds > timeout)
                    {
                        break;
                    }
                    Thread.Sleep(1000);
                    executeSeconds++;
                }
                var failList = receiveMessageDict.Values.Where(w => !w.Item1).ToList();
            });
            receiver.Subscribe();
        }

        public void TestRpcServer()
        {
            var methodNames = new string[] {
                "method1"
                ,"method2"
            };
            IRpcServer receiver = new RpcServer(_conn, methodNames, "rpc-test", parallelDegree: 10);
            receiver.RegisterMessageHandlerByAssembly(typeof(MessageHandler01).Assembly, "ERP.External.Commands");
            receiver.OnMessageHandlingFailed += (sender, args) =>
            {
                //TODO:持久化消息处理失败的信息，以备后续之后重试
            };
            var receiveMessageDict = new ConcurrentDictionary<string, Tuple<bool, IMessageTransportationContext>>();
            receiver.OnMessageReceived += (sender, e) =>
            {
                receiveMessageDict.TryAdd(e.Context.GetMessageId(), new Tuple<bool, IMessageTransportationContext>(false, e.Context));
            };
            receiver.OnMessageHandlingSucceeded += (sender, e) =>
            {
                receiveMessageDict.AddOrUpdate(e.Context.GetMessageId(), new Tuple<bool, IMessageTransportationContext>(true, e.Context), (key, originVal) => new Tuple<bool, IMessageTransportationContext>(true, originVal.Item2));
            };

            //Task.Run(() =>
            //{
            //    var timeout = 100;
            //    var executeSeconds = 0;
            //    while (receiveMessageDict.Count == 0 || receiveMessageDict.Values.Any(w => !w.Item1 && w.Item2.LastException == null))
            //    {
            //        if (executeSeconds > timeout)
            //        {
            //            break;
            //        }
            //        Thread.Sleep(1000);
            //        executeSeconds++;
            //    }
            //    var failList = receiveMessageDict.Values.Where(w => !w.Item1).ToList();
            //});
            receiver.ReceiveRequest();
        }
    }
}
