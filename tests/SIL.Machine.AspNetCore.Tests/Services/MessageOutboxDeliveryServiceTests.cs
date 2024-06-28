namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class MessageOutboxDeliveryServiceTests
{
    private const string OutboxId = "TestOutbox";
    private const string Method1 = "Method1";
    private const string Method2 = "Method2";

    [Test]
    public async Task ProcessMessagesAsync()
    {
        var env = new TestEnvironment();
        env.AddStandardMessages();
        await env.ProcessMessagesAsync();
        Received.InOrder(() =>
        {
            env.Handler.HandleMessageAsync(Method2, "B", null, Arg.Any<CancellationToken>());
            env.Handler.HandleMessageAsync(Method1, "A", null, Arg.Any<CancellationToken>());
            env.Handler.HandleMessageAsync(Method2, "C", null, Arg.Any<CancellationToken>());
        });
        Assert.That(env.Messages.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task ProcessMessagesAsync_Timeout()
    {
        var env = new TestEnvironment();
        env.AddStandardMessages();

        // Timeout is long enough where the message attempt will be incremented, but not deleted.
        env.EnableHandlerFailure(StatusCode.Internal);
        await env.ProcessMessagesAsync();
        // Each group should try to send one message
        Assert.That(env.Messages.Get("B").Attempts, Is.EqualTo(1));
        Assert.That(env.Messages.Get("A").Attempts, Is.EqualTo(0));
        Assert.That(env.Messages.Get("C").Attempts, Is.EqualTo(1));

        // with now shorter timeout, the messages will be deleted.
        // 4 start build attempts, and only one build completed attempt
        env.Options.CurrentValue.Returns(
            new MessageOutboxOptions { MessageExpirationTimeout = TimeSpan.FromMilliseconds(1) }
        );
        await env.ProcessMessagesAsync();
        Assert.That(env.Messages.Count, Is.EqualTo(0));
        _ = env
            .Handler.Received(1)
            .HandleMessageAsync(Method1, Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        _ = env
            .Handler.Received(4)
            .HandleMessageAsync(Method2, Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessMessagesAsync_UnavailableFailure()
    {
        var env = new TestEnvironment();
        env.AddStandardMessages();

        env.EnableHandlerFailure(StatusCode.Unavailable);
        await env.ProcessMessagesAsync();
        // Only the first group should be attempted - but not recorded as attempted
        Assert.That(env.Messages.Get("B").Attempts, Is.EqualTo(0));
        Assert.That(env.Messages.Get("A").Attempts, Is.EqualTo(0));
        Assert.That(env.Messages.Get("C").Attempts, Is.EqualTo(0));
        _ = env
            .Handler.Received(1)
            .HandleMessageAsync(Method2, Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());

        env.Handler.ClearReceivedCalls();
        env.EnableHandlerFailure(StatusCode.Internal);
        await env.ProcessMessagesAsync();
        Assert.That(env.Messages.Get("B").Attempts, Is.EqualTo(1));
        Assert.That(env.Messages.Get("A").Attempts, Is.EqualTo(0));
        Assert.That(env.Messages.Get("C").Attempts, Is.EqualTo(1));
        _ = env
            .Handler.Received(2)
            .HandleMessageAsync(Method2, Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());

        env.Handler.ClearReceivedCalls();
        env.DisableHandlerFailure();
        await env.ProcessMessagesAsync();
        Assert.That(env.Messages.Count, Is.EqualTo(0));
        _ = env
            .Handler.Received(1)
            .HandleMessageAsync(Method1, Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        _ = env
            .Handler.Received(2)
            .HandleMessageAsync(Method2, Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessMessagesAsync_File()
    {
        var env = new TestEnvironment();
        env.AddContentStreamMessages();

        await env.ProcessMessagesAsync();
        Assert.That(env.Messages.Count, Is.EqualTo(0));
        _ = env
            .Handler.Received(1)
            .HandleMessageAsync(Method1, "A", Arg.Is<Stream?>(s => s != null), Arg.Any<CancellationToken>());
        env.FileSystem.Received().DeleteFile(Path.Combine("outbox", "A"));
    }

    private class TestEnvironment
    {
        public TestEnvironment()
        {
            Outboxes = new MemoryRepository<Outbox>();
            Messages = new MemoryRepository<OutboxMessage>();

            Handler = Substitute.For<IOutboxMessageHandler>();
            Handler.OutboxId.Returns(OutboxId);
            FileSystem = Substitute.For<IFileSystem>();
            Options = Substitute.For<IOptionsMonitor<MessageOutboxOptions>>();
            Options.CurrentValue.Returns(new MessageOutboxOptions());

            Service = new MessageOutboxDeliveryService(
                Substitute.For<IServiceProvider>(),
                [Handler],
                FileSystem,
                Options,
                Substitute.For<ILogger<MessageOutboxDeliveryService>>()
            );
        }

        public MemoryRepository<Outbox> Outboxes { get; }
        public MemoryRepository<OutboxMessage> Messages { get; }
        public MessageOutboxDeliveryService Service { get; }
        public IOutboxMessageHandler Handler { get; }
        public IOptionsMonitor<MessageOutboxOptions> Options { get; }
        public IFileSystem FileSystem { get; }

        public Task ProcessMessagesAsync()
        {
            return Service.ProcessMessagesAsync(Messages);
        }

        public void AddStandardMessages()
        {
            // messages out of order - will be fixed when retrieved
            Messages.Add(
                new OutboxMessage
                {
                    Id = "A",
                    Index = 2,
                    Method = Method1,
                    GroupId = "A",
                    OutboxRef = OutboxId,
                    Content = "A",
                    HasContentStream = false
                }
            );
            Messages.Add(
                new OutboxMessage
                {
                    Id = "B",
                    Index = 1,
                    Method = Method2,
                    OutboxRef = OutboxId,
                    GroupId = "A",
                    Content = "B",
                    HasContentStream = false
                }
            );
            Messages.Add(
                new OutboxMessage
                {
                    Id = "C",
                    Index = 3,
                    Method = Method2,
                    OutboxRef = OutboxId,
                    GroupId = "B",
                    Content = "C",
                    HasContentStream = false
                }
            );
        }

        public void AddContentStreamMessages()
        {
            // messages out of order - will be fixed when retrieved
            Messages.Add(
                new OutboxMessage
                {
                    Id = "A",
                    Index = 2,
                    Method = Method1,
                    GroupId = "A",
                    OutboxRef = OutboxId,
                    Content = "A",
                    HasContentStream = true
                }
            );
            FileSystem
                .OpenRead(Path.Combine("outbox", "A"))
                .Returns(ci => new MemoryStream(Encoding.UTF8.GetBytes("Content")));
        }

        public void EnableHandlerFailure(StatusCode code)
        {
            Handler
                .HandleMessageAsync(Method1, Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new RpcException(new Status(code, "")));
            Handler
                .HandleMessageAsync(Method2, Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new RpcException(new Status(code, "")));
        }

        public void DisableHandlerFailure()
        {
            Handler
                .HandleMessageAsync(Method1, Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            Handler
                .HandleMessageAsync(Method2, Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
        }
    }
}
