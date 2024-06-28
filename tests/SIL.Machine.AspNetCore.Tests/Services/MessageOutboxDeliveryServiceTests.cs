using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NSubstitute.ExceptionExtensions;
using Serval.Translation.V1;

namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class MessageOutboxDeliveryServiceTests
{
    [Test]
    public async Task SendMessages()
    {
        var env = new TestEnvironment();
        env.AddStandardMessages();
        await env.MessageOutboxDeliveryService.ProcessMessagesOnceAsync();
        Received.InOrder(() =>
        {
            env.Client.BuildStartedAsync(new BuildStartedRequest { BuildId = "A" });
            env.Client.BuildCompletedAsync(Arg.Any<BuildCompletedRequest>());
            env.Client.BuildStartedAsync(new BuildStartedRequest { BuildId = "B" });
        });
    }

    [Test]
    public async Task SendMessages_Timeout()
    {
        var env = new TestEnvironment();
        env.AddStandardMessages();

        // Timeout is long enough where the message attempt will be incremented, but not deleted.
        env.ClientInternalFailure();
        await Task.Delay(100);
        await env.MessageOutboxDeliveryService.ProcessMessagesOnceAsync();
        // Each group should try to send one message
        Assert.That((await env.Messages.GetAsync(m => m.Id == "B"))!.Attempts, Is.EqualTo(1));
        Assert.That((await env.Messages.GetAsync(m => m.Id == "A"))!.Attempts, Is.EqualTo(0));
        Assert.That((await env.Messages.GetAsync(m => m.Id == "C"))!.Attempts, Is.EqualTo(1));

        // with now shorter timeout, the messages will be deleted.
        // 4 start build attempts, and only one build completed attempt
        env.MessageOutboxDeliveryService.SetMessageExpiration(TimeSpan.FromMilliseconds(1));
        await env.MessageOutboxDeliveryService.ProcessMessagesOnceAsync();
        Assert.That(await env.Messages.ExistsAsync(m => true), Is.False);
        var startCalls = env
            .Client.ReceivedCalls()
            .Count(x => x.GetMethodInfo().Name == nameof(env.Client.BuildStartedAsync));
        Assert.That(startCalls, Is.EqualTo(4));
        var completedCalls = env
            .Client.ReceivedCalls()
            .Count(x => x.GetMethodInfo().Name == nameof(env.Client.BuildCompletedAsync));
        Assert.That(completedCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task SendMessagesUnavailable_Failure()
    {
        var env = new TestEnvironment();
        env.AddStandardMessages();
        env.ClientUnavailableFailure();
        await env.MessageOutboxDeliveryService.ProcessMessagesOnceAsync();
        // Only the first group should be attempted - but not recorded as attempted
        Assert.That((await env.Messages.GetAsync(m => m.Id == "B"))!.Attempts, Is.EqualTo(0));
        Assert.That((await env.Messages.GetAsync(m => m.Id == "A"))!.Attempts, Is.EqualTo(0));
        Assert.That((await env.Messages.GetAsync(m => m.Id == "C"))!.Attempts, Is.EqualTo(0));
        env.ClientInternalFailure();
        await env.MessageOutboxDeliveryService.ProcessMessagesOnceAsync();
        Assert.That((await env.Messages.GetAsync(m => m.Id == "B"))!.Attempts, Is.EqualTo(1));
        Assert.That((await env.Messages.GetAsync(m => m.Id == "A"))!.Attempts, Is.EqualTo(0));
        Assert.That((await env.Messages.GetAsync(m => m.Id == "C"))!.Attempts, Is.EqualTo(1));
        env.ClientNoFailure();
        await env.MessageOutboxDeliveryService.ProcessMessagesOnceAsync();
        Assert.That(await env.Messages.ExistsAsync(m => true), Is.False);
        // 1 (unavailable) + 2 (internal) + 3 (success) = 6 calls
        Assert.That(env.Client.ReceivedCalls().Count(), Is.EqualTo(6));
    }

    [Test]
    public async Task LargeMessageContent()
    {
        var env = new TestEnvironment();
        // large max document size - message not saved to file
        var fileIdC = await env.OutboxService.EnqueueMessageAsync(
            method: ServalPlatformMessageMethod.BuildStarted,
            groupId: "C",
            requestContent: JsonSerializer.Serialize(new BuildStartedRequest { BuildId = "C" }),
            cancellationToken: CancellationToken.None
        );
        Assert.That(await env.SharedFileService.ExistsAsync($"outbox/{fileIdC}"), Is.False);
        await env.MessageOutboxDeliveryService.ProcessMessagesOnceAsync();
        // small max document size - throws error
        env.OutboxService.SetMaxDocumentSize(1);
        Assert.ThrowsAsync<ArgumentException>(
            () =>
                env.OutboxService.EnqueueMessageAsync(
                    method: ServalPlatformMessageMethod.BuildStarted,
                    groupId: "D",
                    requestContent: JsonSerializer.Serialize(new BuildStartedRequest { BuildId = "D" }),
                    cancellationToken: CancellationToken.None
                )
        );
    }

    [Test]
    public async Task PretranslateSaveFile()
    {
        var env = new TestEnvironment();
        // large max document size - message not saved to file
        string pretranslationsPath = "build/C/pretranslations.json";
        using (StreamWriter sw = new(await env.SharedFileService.OpenWriteAsync(pretranslationsPath)))
        {
            sw.WriteLine("[]");
        }
        var fileIdC = await env.OutboxService.EnqueueMessageAsync(
            method: ServalPlatformMessageMethod.InsertPretranslations,
            groupId: "C",
            requestContent: "engineId",
            requestContentPath: pretranslationsPath,
            cancellationToken: CancellationToken.None
        );
        Assert.That(await env.SharedFileService.ExistsAsync(pretranslationsPath), Is.False);
        Assert.That(await env.SharedFileService.ExistsAsync($"outbox/{fileIdC}"), Is.True);
        await env.MessageOutboxDeliveryService.ProcessMessagesOnceAsync();
        Assert.That(await env.SharedFileService.ExistsAsync($"outbox/{fileIdC}"), Is.False);
    }

    private static IOptionsMonitor<MessageOutboxOptions> GetMessageOutboxOptionsMonitor()
    {
        var options = new MessageOutboxOptions();
        var optionsMonitor = Substitute.For<IOptionsMonitor<MessageOutboxOptions>>();
        optionsMonitor.CurrentValue.Returns(options);
        return optionsMonitor;
    }

    public class TestMessageOutboxDeliveryService(
        IRepository<OutboxMessage> messages,
        IEnumerable<IOutboxMessageHandler> outboxMessageHandlers,
        ILogger<MessageOutboxDeliveryService> logger
    ) : MessageOutboxDeliveryService(messages, outboxMessageHandlers, GetMessageOutboxOptionsMonitor(), logger)
    {
        public async Task ProcessMessagesOnceAsync() => await ProcessMessagesAsync();

        public void SetMessageExpiration(TimeSpan messageExpiration) => MessageExpiration = messageExpiration;
    }

    public class TestMessageOutboxService(
        IRepository<Outbox> messageIndexes,
        IRepository<OutboxMessage> messages,
        ISharedFileService sharedFileService
    ) : MessageOutboxService(messageIndexes, messages, sharedFileService)
    {
        public void SetMaxDocumentSize(int maxDocumentSize) => MaxDocumentSize = maxDocumentSize;
    }

    private class TestEnvironment : ObjectModel.DisposableBase
    {
        public MemoryRepository<Outbox> MessageIndexes { get; }
        public MemoryRepository<OutboxMessage> Messages { get; }
        public TestMessageOutboxService OutboxService { get; }
        public ISharedFileService SharedFileService { get; }
        public TranslationPlatformApi.TranslationPlatformApiClient Client { get; }
        public TestMessageOutboxDeliveryService MessageOutboxDeliveryService { get; }
        public AsyncClientStreamingCall<InsertPretranslationRequest, Empty> InsertPretranslationsCall { get; }

        public TestEnvironment()
        {
            MessageIndexes = new MemoryRepository<Outbox>();
            Messages = new MemoryRepository<OutboxMessage>();
            SharedFileService = new SharedFileService(Substitute.For<ILoggerFactory>());
            OutboxService = new TestMessageOutboxService(MessageIndexes, Messages, SharedFileService);

            InsertPretranslationsCall = Grpc.Core.Testing.TestCalls.AsyncClientStreamingCall(
                Substitute.For<IClientStreamWriter<InsertPretranslationRequest>>(),
                Task.FromResult(new Empty()),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }
            );

            Client = Substitute.For<TranslationPlatformApi.TranslationPlatformApiClient>();
            ClientNoFailure();

            MessageOutboxDeliveryService = new TestMessageOutboxDeliveryService(
                Messages,
                [
                    new ServalPlatformOutboxHandler(
                        Client,
                        SharedFileService,
                        Substitute.For<ILogger<ServalPlatformOutboxHandler>>()
                    )
                ],
                Substitute.For<ILogger<MessageOutboxDeliveryService>>()
            );
        }

        public static AsyncUnaryCall<Empty> GetEmptyUnaryCall() =>
            new AsyncUnaryCall<Empty>(
                Task.FromResult(new Empty()),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }
            );

        public void ClientNoFailure()
        {
            Client.BuildStartedAsync(Arg.Any<BuildStartedRequest>()).Returns(GetEmptyUnaryCall());
            Client.BuildCanceledAsync(Arg.Any<BuildCanceledRequest>()).Returns(GetEmptyUnaryCall());
            Client.BuildFaultedAsync(Arg.Any<BuildFaultedRequest>()).Returns(GetEmptyUnaryCall());
            Client.BuildCompletedAsync(Arg.Any<BuildCompletedRequest>()).Returns(GetEmptyUnaryCall());
            Client
                .IncrementTranslationEngineCorpusSizeAsync(Arg.Any<IncrementTranslationEngineCorpusSizeRequest>())
                .Returns(GetEmptyUnaryCall());
            Client
                .InsertPretranslations(cancellationToken: Arg.Any<CancellationToken>())
                .Returns(InsertPretranslationsCall);
        }

        public void ClientInternalFailure()
        {
            Client
                .BuildStartedAsync(Arg.Any<BuildStartedRequest>())
                .Throws(new RpcException(new Status(StatusCode.Internal, "")));
            Client
                .BuildCompletedAsync(Arg.Any<BuildCompletedRequest>())
                .Throws(new RpcException(new Status(StatusCode.Internal, "")));
        }

        public void ClientUnavailableFailure()
        {
            Client
                .BuildStartedAsync(Arg.Any<BuildStartedRequest>())
                .Throws(new RpcException(new Status(StatusCode.Unavailable, "")));
            Client
                .BuildCompletedAsync(Arg.Any<BuildCompletedRequest>())
                .Throws(new RpcException(new Status(StatusCode.Unavailable, "")));
        }

        public void AddStandardMessages()
        {
            // messages out of order - will be fixed when retrieved
            Messages.Add(
                new OutboxMessage
                {
                    Id = "A",
                    Index = 2,
                    Method = ServalPlatformMessageMethod.BuildCompleted.ToString(),
                    GroupId = "A",
                    OutboxName = typeof(ServalPlatformMessageMethod).ToString(),
                    RequestContent = JsonSerializer.Serialize(
                        new BuildCompletedRequest
                        {
                            BuildId = "A",
                            CorpusSize = 100,
                            Confidence = 0.5
                        }
                    )
                }
            );
            Messages.Add(
                new OutboxMessage
                {
                    Id = "B",
                    Index = 1,
                    Method = ServalPlatformMessageMethod.BuildStarted.ToString(),
                    OutboxName = typeof(ServalPlatformMessageMethod).ToString(),
                    GroupId = "A",
                    RequestContent = JsonSerializer.Serialize(new BuildStartedRequest { BuildId = "A" })
                }
            );
            Messages.Add(
                new OutboxMessage
                {
                    Id = "C",
                    Index = 3,
                    Method = ServalPlatformMessageMethod.BuildStarted.ToString(),
                    OutboxName = typeof(ServalPlatformMessageMethod).ToString(),
                    GroupId = "B",
                    RequestContent = JsonSerializer.Serialize(new BuildStartedRequest { BuildId = "B" })
                }
            );
        }
    }
}
