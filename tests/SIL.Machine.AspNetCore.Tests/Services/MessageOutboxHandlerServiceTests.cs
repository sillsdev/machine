using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NSubstitute.ExceptionExtensions;
using Serval.Translation.V1;

namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class MessageOutboxHandlerServiceTests
{
    [Test]
    public async Task SendMessages()
    {
        var env = new TestEnvironment();
        env.AddStandardMessages();
        await env.MessageOutboxHandlerService.ProcessMessagesOnceAsync();
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
        await env.MessageOutboxHandlerService.ProcessMessagesOnceAsync();
        // Each group should try to send one message
        Assert.That((await env.Messages.GetAsync(m => m.Id == "1"))!.Attempts, Is.EqualTo(1));
        Assert.That((await env.Messages.GetAsync(m => m.Id == "2"))!.Attempts, Is.EqualTo(0));
        Assert.That((await env.Messages.GetAsync(m => m.Id == "3"))!.Attempts, Is.EqualTo(1));

        // with now shorter timeout, the messages will be deleted.
        // 4 start build attempts, and only one build completed attempt
        env.MessageOutboxHandlerService.SetMessageExpiration(TimeSpan.FromMilliseconds(1));
        await env.MessageOutboxHandlerService.ProcessMessagesOnceAsync();
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
        await env.MessageOutboxHandlerService.ProcessMessagesOnceAsync();
        // Only the first group should be attempted - but not recorded as attempted
        Assert.That((await env.Messages.GetAsync(m => m.Id == "1"))!.Attempts, Is.EqualTo(0));
        Assert.That((await env.Messages.GetAsync(m => m.Id == "2"))!.Attempts, Is.EqualTo(0));
        Assert.That((await env.Messages.GetAsync(m => m.Id == "3"))!.Attempts, Is.EqualTo(0));
        env.ClientInternalFailure();
        await env.MessageOutboxHandlerService.ProcessMessagesOnceAsync();
        Assert.That((await env.Messages.GetAsync(m => m.Id == "1"))!.Attempts, Is.EqualTo(1));
        Assert.That((await env.Messages.GetAsync(m => m.Id == "2"))!.Attempts, Is.EqualTo(0));
        Assert.That((await env.Messages.GetAsync(m => m.Id == "3"))!.Attempts, Is.EqualTo(1));
        env.ClientNoFailure();
        await env.MessageOutboxHandlerService.ProcessMessagesOnceAsync();
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
            method: OutboxMessageMethod.BuildStarted,
            groupId: "C",
            requestContent: JsonSerializer.Serialize(new BuildStartedRequest { BuildId = "C" }),
            CancellationToken.None
        );
        Assert.That(await env.SharedFileService.ExistsAsync($"outbox/{fileIdC}.json"), Is.False);
        await env.MessageOutboxHandlerService.ProcessMessagesOnceAsync();
        // small max document size - message saved to file
        env.OutboxService.SetMaxDocumentSize(1);
        var fileIdD = await env.OutboxService.EnqueueMessageAsync(
            method: OutboxMessageMethod.BuildStarted,
            groupId: "D",
            requestContent: JsonSerializer.Serialize(new BuildStartedRequest { BuildId = "D" }),
            CancellationToken.None
        );
        Assert.That(await env.SharedFileService.ExistsAsync($"outbox/{fileIdD}.json"), Is.True);
        await env.MessageOutboxHandlerService.ProcessMessagesOnceAsync();
        Assert.That(await env.SharedFileService.ExistsAsync($"outbox/{fileIdD}.json"), Is.False);
    }

    public class TestMessageOutboxHandlerService(
        TranslationPlatformApi.TranslationPlatformApiClient client,
        IRepository<OutboxMessage> messages,
        ISharedFileService sharedFileService,
        ILogger<MessageOutboxHandlerService> logger
    ) : MessageOutboxHandlerService(client, messages, sharedFileService, logger)
    {
        public async Task ProcessMessagesOnceAsync() => await ProcessMessagesAsync();

        public void SetMessageExpiration(TimeSpan messageExpiration) => MessageExpiration = messageExpiration;
    }

    public class TestMessageOutboxService(IRepository<OutboxMessage> messages, ISharedFileService sharedFileService)
        : MessageOutboxService(messages, sharedFileService)
    {
        public void SetMaxDocumentSize(int maxDocumentSize) => MaxDocumentSize = maxDocumentSize;
    }

    private class TestEnvironment : ObjectModel.DisposableBase
    {
        public MemoryRepository<OutboxMessage> Messages { get; }
        public TestMessageOutboxService OutboxService { get; }
        public ISharedFileService SharedFileService { get; }
        public TranslationPlatformApi.TranslationPlatformApiClient Client { get; }
        public TestMessageOutboxHandlerService MessageOutboxHandlerService { get; }
        public AsyncClientStreamingCall<InsertPretranslationRequest, Empty> InsertPretranslationsCall { get; }

        public TestEnvironment()
        {
            Messages = new MemoryRepository<OutboxMessage>();
            SharedFileService = new SharedFileService(Substitute.For<ILoggerFactory>());
            OutboxService = new TestMessageOutboxService(Messages, SharedFileService);

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

            MessageOutboxHandlerService = new TestMessageOutboxHandlerService(
                Client,
                Messages,
                SharedFileService,
                Substitute.For<ILogger<MessageOutboxHandlerService>>()
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
                    Id = "2",
                    Method = OutboxMessageMethod.BuildCompleted,
                    GroupId = "A",
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
                    Id = "1",
                    Method = OutboxMessageMethod.BuildStarted,
                    GroupId = "A",
                    RequestContent = JsonSerializer.Serialize(new BuildStartedRequest { BuildId = "A" })
                }
            );
            Messages.Add(
                new OutboxMessage
                {
                    Id = "3",
                    Method = OutboxMessageMethod.BuildStarted,
                    GroupId = "B",
                    RequestContent = JsonSerializer.Serialize(new BuildStartedRequest { BuildId = "B" })
                }
            );
        }
    }
}
