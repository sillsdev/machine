namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class MessageOutboxServiceTests
{
    private const string OutboxId = "TestOutbox";
    private const string Method = "TestMethod";

    [Test]
    public async Task EnqueueMessageAsync_NoContentStream()
    {
        TestEnvironment env = new();

        await env.Service.EnqueueMessageAsync(OutboxId, Method, "A", "content");

        Outbox outbox = env.Outboxes.Get(OutboxId);
        Assert.That(outbox.CurrentIndex, Is.EqualTo(1));

        OutboxMessage message = env.Messages.Get("1");
        Assert.That(message.OutboxRef, Is.EqualTo(OutboxId));
        Assert.That(message.Method, Is.EqualTo(Method));
        Assert.That(message.Index, Is.EqualTo(1));
        Assert.That(message.Content, Is.EqualTo("content"));
        Assert.That(message.HasContentStream, Is.False);
    }

    [Test]
    public async Task EnqueueMessageAsync_ExistingOutbox()
    {
        TestEnvironment env = new();
        env.Outboxes.Add(new Outbox { Id = OutboxId, CurrentIndex = 1 });

        await env.Service.EnqueueMessageAsync(OutboxId, Method, "A", "content");

        Outbox outbox = env.Outboxes.Get(OutboxId);
        Assert.That(outbox.CurrentIndex, Is.EqualTo(2));

        OutboxMessage message = env.Messages.Get("1");
        Assert.That(message.OutboxRef, Is.EqualTo(OutboxId));
        Assert.That(message.Method, Is.EqualTo(Method));
        Assert.That(message.Index, Is.EqualTo(2));
        Assert.That(message.Content, Is.EqualTo("content"));
        Assert.That(message.HasContentStream, Is.False);
    }

    [Test]
    public async Task EnqueueMessageAsync_HasContentStream()
    {
        TestEnvironment env = new();
        await using MemoryStream fileStream = new();
        env.FileSystem.OpenWrite(Path.Combine("outbox", "1")).Returns(fileStream);

        await using MemoryStream stream = new(Encoding.UTF8.GetBytes("content"));
        await env.Service.EnqueueMessageAsync(OutboxId, Method, "A", "content", stream);

        OutboxMessage message = env.Messages.Get("1");
        Assert.That(message.OutboxRef, Is.EqualTo(OutboxId));
        Assert.That(message.Method, Is.EqualTo(Method));
        Assert.That(message.Index, Is.EqualTo(1));
        Assert.That(message.Content, Is.EqualTo("content"));
        Assert.That(message.HasContentStream, Is.True);
        Assert.That(fileStream.ToArray(), Is.EqualTo(stream.ToArray()));
    }

    [Test]
    public void EnqueueMessageAsync_ContentTooLarge()
    {
        TestEnvironment env = new();
        env.Service.MaxDocumentSize = 5;

        Assert.ThrowsAsync<ArgumentException>(() => env.Service.EnqueueMessageAsync(OutboxId, Method, "A", "content"));
    }

    private class TestEnvironment
    {
        public TestEnvironment()
        {
            Outboxes = new MemoryRepository<Outbox>();
            Messages = new MemoryRepository<OutboxMessage>();
            var idGenerator = Substitute.For<IIdGenerator>();
            idGenerator.GenerateId().Returns("1");
            FileSystem = Substitute.For<IFileSystem>();
            var options = Substitute.For<IOptionsMonitor<MessageOutboxOptions>>();
            options.CurrentValue.Returns(new MessageOutboxOptions());
            Service = new MessageOutboxService(Outboxes, Messages, idGenerator, FileSystem, options);
        }

        public MemoryRepository<Outbox> Outboxes { get; }
        public MemoryRepository<OutboxMessage> Messages { get; }
        public IFileSystem FileSystem { get; }
        public MessageOutboxService Service { get; }
    }
}
