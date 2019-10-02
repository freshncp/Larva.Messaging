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
                throw new System.Exception("Save fail");
            };

            receiver.SubscribeAll();
        }

        public void TestPubsub(string queueName)
        {
            IPubsubReceiver receiver = new PubsubReceiver(_conn, queueName, parallelDegree: 16, debugEnabled: true);
            receiver.RegisterMessageHandlerByAssembly(typeof(MessageHandler01).Assembly);
            receiver.OnMessageHandlingFailed += (sender, args) =>
            {
                //TODO:持久化消息处理失败的信息，以备后续之后重试
                Console.WriteLine(args.Context.LastException.Message + "\r\n\t" + args.Context.LastException.StackTrace);
                throw new System.Exception("Save fail");
            };
     
            receiver.Subscribe();
        }

        public void TestPubsub2()
        {
            var exchangeName = "test.pubsub2";
            var subscriberNames = new string[] { "wms", "erp" };
            byte queueCount = 2;
            IPubsubSender publisher = new PubsubSender(_conn, exchangeName, subscriberNames, atLeastMatchOneQueue: true, publishToExchange: true, publishToExchangeQueueCount: queueCount, debugEnabled: true);
            
            foreach(var subscriberName in publisher.SubscriberNameQueueOrExchangeNameMapping.Keys)
            {
                new TopicSender(_conn, publisher.SubscriberNameQueueOrExchangeNameMapping[subscriberName], publisher.PublishToExchangeQueueCount, sourceExchangeName: exchangeName, debugEnabled: true);
                ITopicReceiver receiver = new TopicReceiver(_conn, publisher.SubscriberNameQueueOrExchangeNameMapping[subscriberName], publisher.PublishToExchangeQueueCount, debugEnabled: true);
                receiver.RegisterMessageHandlerByAssembly(typeof(MessageHandler01).Assembly);
                receiver.OnMessageHandlingFailed += (sender, args) =>
                {
                    //TODO:持久化消息处理失败的信息，以备后续之后重试
                    Console.WriteLine(args.Context.LastException.Message + "\r\n\t" + args.Context.LastException.StackTrace);
                    throw new System.Exception("Save fail");
                };
     
                receiver.SubscribeAll();
            }
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
                Console.WriteLine(args.Context.LastException.Message + "\r\n\t" + args.Context.LastException.StackTrace);
                throw new System.Exception("Save fail");
            };

            receiver.ReceiveRequest();
        }
    }
}
