# Larva.Messaging
基于MQ的消息处理，支持专题模式、发布订阅模式和Rpc模式

- 支持消息路由、顺序消费；
- 支持部署多台消费端程序（相同Exchange下，一个队列仅允许被一个消费端程序消费；但一个消费端程序可以同时消费多个队列）；
- 采用Envelope<T>进行消息封装，通过实现接口IMessageHandler<T>进行消息处理；
- 消息发送，默认发送到mq失败时会不断重试，也可以通过初始化TopicSender时显式指定重试次数；
- 同一个队列，如果消息处理失败，则会不断重试，直到达到最大重试次数；
- 支持专题模式（Topic）、发布订阅模式（Pubsub）和Rpc模式；
- 发送端和消息端使用的消息类之间解耦，可以通过MessageTypeAttribute对消息类进行消息类型声明，默认为带名字空间的类名；
- 引入发送和处理的C#事件，方便业务端处理；
- 消费端消费失败，且OnMessageHandlingFailed事件处理失败时，利用rabbitmq的DLX特性，丢到一个DLX Exchange（使用默认Exchange）中，DLX Exchange发送的队列的命名=Exchange名 + "-dlx"，发布订阅模式需要手工建；

## 调用示例

- 定义消息

```csharp
// 可以定义消息类型名，不设置，默认取带名字空间的类名
//[MessageType(TypeName = "Tests.Messages.1")]
public class Message01
{
    public Guid HostingFilialeId { get; set; }

    public Message01() { }

    public Message01(Guid hostingFilialeId)
    {
        HostingFilialeId = hostingFilialeId;
    }
}
```

- 消息发送

```csharp
// 创建 Rabbitmq 连接（此为全局变量，一个应用共享一个连接即可）
var conn = new Connection(new ConnectionConfig(new Uri("amqp://user:pwd@192.168.117.21:5673/virtual_host")));

// 创建Topic发送者（此为全局变量，一个对象实例对应使用一个rabbitmq的exchange）
ITopicSender publisher = new TopicSender(conn, exchangeName, queueCount);
//如果采用发布订阅方式，则如下声明
//IPubsubSender publisher = new PubsubSender(conn, exchangeName);
//如果采用RPC方式，则如下声明
//IRpcClient client = new RpcClient(conn);
//client.SendRequest(Envelope.Create(message), "<定义的方法名>");

// 订阅消息处理失败的事件
publisher.OnMessageSendingFailed += (sender, args) =>
{
	//TODO:持久化发送失败的信息，目前通过异常形式作为后续重试，以后可能会改成以事件方式，用于重试
};

// 订阅消息已发送的事件
receiver.OnMessageSent += (sender, args) =>
{
	//TODO:持久化已发送的消息，结合 OnMessageSendingSucceeded ，用于判断哪些消息未成功发送
};

// 订阅消息已成功发送的事件
receiver.OnMessageSendingSucceeded += (sender, args) =>
{
	//TODO:持久化已成功发送的消息，结合 OnMessageSent ，用于判断哪些消息未成功发送
};

// 发送消息
var message = new Message01(new Guid("58437EDC-87B7-4995-A5C0-BB5FD0FE49E0"));
publisher.SendMessage(Envelope.Create(message, message.HostingFilialeId.ToString()));

// 释放
publisher.Dispose();
```

- 消息消费

```csharp
// 定义消息处理器
// 注意：一个处理器类，可以定义为多个消息类型的处理器；但针对同一个Exchange，一个消息类型仅允许对应一个消息处理器
[MessageHandlerType(Category = "ERP.External.Commands")]
public class MessageHandler01 : IMessageHandler<Message01>, IMessageHandler<Message02>
{
    public void Handle(Message01 message, IMessageTransportationContext context)
    {
        // 处理消息 Message01
    }

    public void Handle(Message02 message, IMessageTransportationContext context)
    {
        // 处理消息 Message02
    }
}
```

```csharp
// 创建 Rabbitmq 连接（此为全局变量，一个应用共享一个连接即可）
var conn = new Connection(new ConnectionConfig(new Uri("amqp://user:pwd@192.168.117.21:5673/virtual_host")));

// 创建接受者
ITopicReceiver receiver = new TopicReceiver(conn, exchangeName, queueCount);
//如果采用发布订阅方式，则如下声明
//IPubsubReceiver receiver = new PubsubReceiver(conn, queueName);
//如果采用RPC方式，则如下声明
//IRpcServer server = new RpcServer(conn, new string[] { "method1", "method2" });
//server.ReceiveRequest();

// 注册消息处理器
receiver.RegisterMessageHandler<MessageHandler01>();
//如果按指定程序集下注册所有的消息处理器，可以如下调用
//receiver.RegisterMessageHandlerByAssembly(typeof(MessageHandler01).Assembly);
//如果按指定程序集下注册指定消息类别的消息处理器，可以如下调用
//receiver.RegisterMessageHandlerByAssembly(typeof(MessageHandler01).Assembly, ""ERP.External.Commands"");

// 订阅消息处理失败的事件
receiver.OnMessageHandlingFailed += (sender, args) =>
{
	//TODO:持久化消息处理失败的信息，以备后续之后重试
};

// 订阅消息已接收的事件
receiver.OnMessageReceived += (sender, args) =>
{
	//TODO:持久化已接收的消息，结合 OnMessageHandlingSucceeded ，用于判断哪些消息未成功执行
};

// 订阅消息已成功处理的事件
receiver.OnMessageHandlingSucceeded += (sender, args) =>
{
	//TODO:持久化已成功处理的消息，结合 OnMessageReceived ，用于判断哪些消息未成功执行
};

// 订阅所有队列
receiver.SubscribeAll();
// 如果采用分布式部署多台消费端后端程序，则通过指定队列序号来实现
// 注意：同一个队列，仅允许一个后端程序；但一个后端程序可以消费多个队列
// receiver.Subscribe(0);
//如果采用发布订阅方式，则如下调用
//receiver.Subscribe();
```

## 更新历史

### 1.0.0 (更新日期：2019/9/24)

```plain
1）支持消息路由、顺序消费；
2）支持部署多台消费端程序（相同Exchange下，一个队列仅允许被一个消费端程序消费；但一个消费端程序可以同时消费多个队列）；
3）采用Envelope<T>进行消息封装，通过实现接口IMessageHandler<T>进行消息处理；；
4）消息发送，默认发送到mq失败时会不断重试，也可以通过初始化TopicSender时显式指定重试次数；
5）同一个队列，如果消息处理失败，则会不断重试，直到显式标记为丢弃消息(通过IMessageHandler.DropMessageOnFailed方法返回true来标记)；
6）支持专题模式（Topic）、发布订阅模式（Pubsub）和Rpc模式；
7）发送端和消息端使用的消息类之间解耦，可以通过MessageTypeAttribute对消息类进行消息类型声明，默认为带名字空间的类名；
8）引入发送和处理的C#事件，方便业务端处理；
9）消费端消费失败，且OnMessageHandlingFailed事件处理失败时，利用rabbitmq的DLX特性，丢到一个DLX Exchange（使用默认Exchange）中，DLX Exchange发送的队列的命名=Exchange名 + "-dlx"，发布订阅模式需要手工建；
```
