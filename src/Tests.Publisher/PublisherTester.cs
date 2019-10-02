using Larva.Messaging;
using Larva.Messaging.RabbitMQ;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tests.Messages;

namespace Tests.Publisher
{
    public class PublisherTester : IDisposable
    {
        private Connection _conn;
        private Random random = new Random(DateTime.Now.Millisecond);

        public PublisherTester()
        {
            _conn = new Connection(new ConnectionConfig(new Uri("amqp://test:123456@localhost:5672/test")));
        }

        public void Dispose()
        {
            _conn.Close();
        }

        public void TestTopic()
        {
            var exchangeName = "test.topic";
            byte queueCount = 4;

            ITopicSender publisher = new TopicSender(_conn, exchangeName, queueCount, confirmEnabled: true, debugEnabled: true);
            var sentMessageDict = new ConcurrentDictionary<string, Tuple<bool, IMessageTransportationContext>>();
            publisher.OnMessageSent += (sender, e) =>
            {
                sentMessageDict.TryAdd(e.Context.GetMessageId(), new Tuple<bool, IMessageTransportationContext>(false, e.Context));
            };
            publisher.OnMessageSendingSucceeded += (sender, e) =>
            {
                sentMessageDict.TryRemove(e.Context.GetMessageId(), out Tuple<bool, IMessageTransportationContext> _);
            };
            int sendSequence = 0;
            Console.WriteLine($"{nameof(TestTopic)} start");
            while (true)
            {
                Thread.Sleep(10);
                try
                {
                    sendSequence++;
                    if (sendSequence % 3 == 0)
                    {
                        var message = new Message01(new Guid("58437EDC-87B7-4995-A5C0-BB5FD0FE49E0"))
                        {
                            Sequence = random.Next(1, 20)
                        };
                        Envelope envelopedMessage = Envelope.Create(message, $"{message.HostingFilialeId}_{Guid.NewGuid()}");
                        Task.Run(() => publisher.SendMessage(envelopedMessage));
                    }
                    else if(sendSequence % 3 == 1)
                    {
                        var message = new Message02(new Guid("7AE62AF0-EB1F-49C6-8FD1-128D77C84698"));
                        Task.Run(() => publisher.SendMessage(Envelope.Create(message, $"{message.SaleFilialeId}")));
                    }
                    else
                    {
                        var message = new Message03(new Guid("4AE62AF0-EB1F-49C6-8FD1-128D77C84698"));
                        Task.Run(() => publisher.SendMessage(Envelope.Create(message, $"{message.SaleFilialeId}")));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.GetType().FullName}");
                }
                if (sendSequence == 10000)
                {
                    Thread.Sleep(3000);
                    Task.Run(() =>
                    {
                        publisher.Dispose();
                    });
                    break;
                }
            }
            var timeout = sendSequence / 10;
            var executeSeconds = 0;
            while (sentMessageDict.Count > 0)
            {
                if (executeSeconds > timeout)
                {
                    break;
                }
                Thread.Sleep(1000);
                executeSeconds++;
            }
            var failList = sentMessageDict.Values.ToList();
            Console.WriteLine($"{nameof(TestTopic)} send fail count: {failList.Count}, total: {sendSequence}");
        }

        public void TestPubsub()
        {
            var exchangeName = "test.pubsub";
            var subscriberNames = new string[] { "wms", "erp" };
            IPubsubSender publisher = new PubsubSender(_conn, exchangeName, subscriberNames, debugEnabled: true);
            var sentMessageDict = new ConcurrentDictionary<string, Tuple<bool, IMessageTransportationContext>>();
            publisher.OnMessageSent += (sender, e) =>
            {
                sentMessageDict.TryAdd(e.Context.GetMessageId(), new Tuple<bool, IMessageTransportationContext>(false, e.Context));
            };
            publisher.OnMessageSendingSucceeded += (sender, e) =>
            {
                sentMessageDict.TryRemove(e.Context.GetMessageId(), out Tuple<bool, IMessageTransportationContext> _);
            };
            int sendSequence = 0;
            Console.WriteLine($"{nameof(TestPubsub)} start");
            while (true)
            {
                Thread.Sleep(10);
                try
                {
                    sendSequence++;
                    if (sendSequence % 4 != 0)
                    {
                        var message = new Message01(new Guid("58437EDC-87B7-4995-A5C0-BB5FD0FE49E0"))
                        {
                            Sequence = random.Next(1, 20)
                        };
                        Envelope envelopedMessage = Envelope.Create(message, $"{message.HostingFilialeId}_{Guid.NewGuid()}");
                        Task.Run(() => publisher.SendMessage(envelopedMessage));
                    }
                    else
                    {
                        var message = new Message02(new Guid("7AE62AF0-EB1F-49C6-8FD1-128D77C84698"));
                        Task.Run(() => publisher.SendMessage(Envelope.Create(message, $"{message.SaleFilialeId}")));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                }
                if (sendSequence == 10000)
                {
                    Thread.Sleep(3000);
                    Task.Run(() =>
                    {
                        publisher.Dispose();
                    });
                    break;
                }
            }
            var timeout = sendSequence / 10;
            var executeSeconds = 0;
            while (sentMessageDict.Count > 0)
            {
                if (executeSeconds > timeout)
                {
                    break;
                }
                Thread.Sleep(1000);
                executeSeconds++;
            }
            var failList = sentMessageDict.Values.ToList();
            Console.WriteLine($"{nameof(TestPubsub)} send fail count: {failList.Count}, total: {sendSequence}");
        }

        public void TestPubsub2()
        {
            var exchangeName = "test.pubsub2";
            var subscriberNames = new string[] { "wms", "erp" };
            byte queueCount = 2;
            IPubsubSender publisher = new PubsubSender(_conn, exchangeName, subscriberNames, confirmEnabled: true, atLeastMatchOneQueue: true, publishToExchange: true, publishToExchangeQueueCount: queueCount, debugEnabled: true);
            var sentMessageDict = new ConcurrentDictionary<string, Tuple<bool, IMessageTransportationContext>>();
            publisher.OnMessageSent += (sender, e) =>
            {
                sentMessageDict.TryAdd(e.Context.GetMessageId(), new Tuple<bool, IMessageTransportationContext>(false, e.Context));
            };
            publisher.OnMessageSendingSucceeded += (sender, e) =>
            {
                sentMessageDict.TryRemove(e.Context.GetMessageId(), out Tuple<bool, IMessageTransportationContext> _);
            };
            foreach (var subscriberName in publisher.SubscriberNameQueueOrExchangeNameMapping.Keys)
            {
                var topicPublisher = new TopicSender(_conn, publisher.SubscriberNameQueueOrExchangeNameMapping[subscriberName], queueCount, sourceExchangeName: publisher.ExchangeName);
            }
            int sendSequence = 0;
            Console.WriteLine($"{nameof(TestPubsub2)} start");
            while (true)
            {
                Thread.Sleep(10);
                try
                {
                    sendSequence++;
                    if (sendSequence % 4 != 0)
                    {
                        var message = new Message01(new Guid("58437EDC-87B7-4995-A5C0-BB5FD0FE49E0"))
                        {
                            Sequence = random.Next(1, 20)
                        };
                        Envelope envelopedMessage = Envelope.Create(message, $"{message.HostingFilialeId}_{Guid.NewGuid()}");
                        Task.Run(() => publisher.SendMessage(envelopedMessage));
                    }
                    else
                    {
                        var message = new Message02(new Guid("7AE62AF0-EB1F-49C6-8FD1-128D77C84698"));
                        Task.Run(() => publisher.SendMessage(Envelope.Create(message, $"{message.SaleFilialeId}")));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                }
                if (sendSequence == 10000)
                {
                    Thread.Sleep(3000);
                    Task.Run(() =>
                    {
                        publisher.Dispose();
                    });
                    break;
                }
            }
            var timeout = sendSequence / 10;
            var executeSeconds = 0;
            while (sentMessageDict.Count > 0)
            {
                if (executeSeconds > timeout)
                {
                    break;
                }
                Thread.Sleep(1000);
                executeSeconds++;
            }
            var failList = sentMessageDict.Values.ToList();
            Console.WriteLine($"{nameof(TestPubsub2)} send fail count: {failList.Count}, total: {sendSequence}");
        }

        public void TestRpcClient()
        {
            IRpcClient publisher = new RpcClient(_conn, "rpc-test", confirmEnabled: true, debugEnabled: true);
            var sentMessageDict = new ConcurrentDictionary<string, Tuple<bool, IMessageTransportationContext>> ();
            publisher.OnMessageSent += (sender, e) =>
            {
                sentMessageDict.TryAdd(e.Context.GetMessageId(), new Tuple<bool, IMessageTransportationContext>(false, e.Context));
            };
            publisher.OnMessageSendingSucceeded += (sender, e) =>
            {
                sentMessageDict.TryRemove(e.Context.GetMessageId(), out Tuple<bool, IMessageTransportationContext> _);
            };
            int sendSequence = 0;
            Console.WriteLine($"{nameof(TestRpcClient)} start");
            while (true)
            {
                Thread.Sleep(10);
                try
                {
                    sendSequence++;
                    if (sendSequence % 4 != 0)
                    {
                        var message = new Message01(new Guid("58437EDC-87B7-4995-A5C0-BB5FD0FE49E0"))
                        {
                            Sequence = random.Next(1, 20)
                        };
                        var envelopedMessage = Envelope.Create(message);
                        publisher.SendRequest(envelopedMessage, "method1", envelopedMessage.MessageId);
                    }
                    else
                    {
                        var message = new Message02(new Guid("7AE62AF0-EB1F-49C6-8FD1-128D77C84698"));
                        publisher.SendRequest(Envelope.Create(message), "method2", Guid.NewGuid().ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                }
                if (sendSequence == 10000)
                {
                    Thread.Sleep(3000);
                    Task.Run(() =>
                    {
                        publisher.Dispose();
                    });
                    break;
                }
            }
            var timeout = sendSequence / 10;
            var executeSeconds = 0;
            while (sentMessageDict.Count > 0)
            {
                if (executeSeconds > timeout)
                {
                    break;
                }
                Thread.Sleep(1000);
                executeSeconds++;
            }
            var failList = sentMessageDict.Values.ToList();
            Console.WriteLine($"{nameof(TestRpcClient)} send fail count: {failList.Count}, total: {sendSequence}");
        }
    }
}
