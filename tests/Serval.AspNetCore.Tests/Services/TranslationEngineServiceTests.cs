using Google.Protobuf.WellKnownTypes;
using Serval.Engine.Translation.V1;

namespace Serval.AspNetCore.Services;

[TestFixture]
public class TranslationEngineServiceTests
{
    [Test]
    public async Task TranslateAsync_EngineDoesNotExist()
    {
        using var env = new TestEnvironment();
        TranslationResult? result = await env.Service.TranslateAsync("engine1", "Esto es una prueba.");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task TranslateAsync_EngineExists()
    {
        using var env = new TestEnvironment();
        string engineId = (await env.CreateEngineAsync()).Id;
        TranslationResult? result = await env.Service.TranslateAsync(engineId, "Esto es una prueba.");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Tokens, Is.EqualTo("this is a test .".Split()));
    }

    [Test]
    public async Task GetWordGraphAsync_EngineDoesNotExist()
    {
        using var env = new TestEnvironment();
        WordGraph? result = await env.Service.GetWordGraphAsync("engine1", "Esto es una prueba.");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetWordGraphAsync_EngineExists()
    {
        using var env = new TestEnvironment();
        string engineId = (await env.CreateEngineAsync()).Id;
        WordGraph? result = await env.Service.GetWordGraphAsync(engineId, "Esto es una prueba.");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Arcs.SelectMany(a => a.Tokens), Is.EqualTo("this is a test .".Split()));
    }

    [Test]
    public async Task TrainSegmentAsync_EngineDoesNotExist()
    {
        using var env = new TestEnvironment();
        bool result = await env.Service.TrainSegmentPairAsync(
            "engine1",
            "Esto es una prueba.",
            "This is a test.",
            true
        );
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task TrainSegmentAsync_EngineExists()
    {
        using var env = new TestEnvironment();
        string engineId = (await env.CreateEngineAsync()).Id;
        bool result = await env.Service.TrainSegmentPairAsync(engineId, "Esto es una prueba.", "This is a test.", true);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CreateAsync()
    {
        using var env = new TestEnvironment();
        var engine = new TranslationEngine
        {
            Id = "engine1",
            SourceLanguageTag = "es",
            TargetLanguageTag = "en",
            Type = "smt"
        };
        await env.Service.CreateAsync(engine);

        engine = (await env.Engines.GetAsync("engine1"))!;
        Assert.That(engine.SourceLanguageTag, Is.EqualTo("es"));
        Assert.That(engine.TargetLanguageTag, Is.EqualTo("en"));
    }

    [Test]
    public async Task DeleteAsync_EngineExists()
    {
        using var env = new TestEnvironment();
        string engineId = (await env.CreateEngineAsync()).Id;
        bool result = await env.Service.DeleteAsync("engine1");
        Assert.That(result, Is.True);
        TranslationEngine? engine = await env.Engines.GetAsync(engineId);
        Assert.That(engine, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_ProjectDoesNotExist()
    {
        using var env = new TestEnvironment();
        await env.CreateEngineAsync();
        bool result = await env.Service.DeleteAsync("engine3");
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task StartBuildAsync_EngineExists()
    {
        using var env = new TestEnvironment();
        string engineId = (await env.CreateEngineAsync()).Id;
        Build? build = await env.Service.StartBuildAsync(engineId);
        Assert.That(build, Is.Not.Null);
    }

    [Test]
    public async Task CancelBuildAsync_EngineExistsNotBuilding()
    {
        using var env = new TestEnvironment();
        string engineId = (await env.CreateEngineAsync()).Id;
        await env.Service.CancelBuildAsync(engineId);
    }

    private class TestEnvironment : DisposableBase
    {
        public TestEnvironment()
        {
            Engines = new MemoryRepository<TranslationEngine>();
            var translationServiceClient = Substitute.For<TranslationService.TranslationServiceClient>();
            var translationResult = new TranslationResult
            {
                Tokens = { "this is a test .".Split() },
                Confidences = { 1.0, 1.0, 1.0, 1.0, 1.0 },
                Sources =
                {
                    (uint)TranslationSources.Smt,
                    (uint)TranslationSources.Smt,
                    (uint)TranslationSources.Smt,
                    (uint)TranslationSources.Smt,
                    (uint)TranslationSources.Smt
                },
                AlignedWordPairs =
                {
                    new AlignedWordPair { SourceIndex = 0, TargetIndex = 0 },
                    new AlignedWordPair { SourceIndex = 1, TargetIndex = 1 },
                    new AlignedWordPair { SourceIndex = 2, TargetIndex = 2 },
                    new AlignedWordPair { SourceIndex = 3, TargetIndex = 3 },
                    new AlignedWordPair { SourceIndex = 4, TargetIndex = 4 }
                },
                Phrases =
                {
                    new Phrase
                    {
                        SourceSegmentStart = 0,
                        SourceSegmentEnd = 5,
                        TargetSegmentCut = 5,
                        Confidence = 1.0
                    }
                }
            };
            var translateResponse = new TranslateResponse();
            translateResponse.Results.Add(translationResult);
            translationServiceClient
                .TranslateAsync(Arg.Any<TranslateRequest>())
                .Returns(CreateAsyncUnaryCall(translateResponse));
            var wordGraph = new WordGraph
            {
                FinalStates = { 3 },
                Arcs =
                {
                    new WordGraphArc
                    {
                        PrevState = 0,
                        NextState = 1,
                        Score = 1.0,
                        Tokens = { "this is".Split() },
                        AlignedWordPairs =
                        {
                            new AlignedWordPair { SourceIndex = 0, TargetIndex = 0 },
                            new AlignedWordPair { SourceIndex = 1, TargetIndex = 1 }
                        },
                        SourceSegmentStart = 0,
                        SourceSegmentEnd = 2,
                        Sources = { GetSources(2, false) },
                        Confidences = { 1.0, 1.0 }
                    },
                    new WordGraphArc
                    {
                        PrevState = 1,
                        NextState = 2,
                        Score = 1.0,
                        Tokens = { "a test".Split() },
                        AlignedWordPairs =
                        {
                            new AlignedWordPair { SourceIndex = 0, TargetIndex = 0 },
                            new AlignedWordPair { SourceIndex = 1, TargetIndex = 1 }
                        },
                        SourceSegmentStart = 2,
                        SourceSegmentEnd = 4,
                        Sources = { GetSources(2, false) },
                        Confidences = { 1.0, 1.0 }
                    },
                    new WordGraphArc
                    {
                        PrevState = 2,
                        NextState = 3,
                        Score = 1.0,
                        Tokens = { new[] { "." } },
                        AlignedWordPairs =
                        {
                            new AlignedWordPair { SourceIndex = 0, TargetIndex = 0 }
                        },
                        SourceSegmentStart = 4,
                        SourceSegmentEnd = 5,
                        Sources = { GetSources(1, false) },
                        Confidences = { 1.0 }
                    }
                }
            };
            var getWordGraphResponse = new GetWordGraphResponse { WordGraph = wordGraph };
            translationServiceClient
                .GetWordGraphAsync(Arg.Any<GetWordGraphRequest>())
                .Returns(CreateAsyncUnaryCall(getWordGraphResponse));
            translationServiceClient
                .CancelBuildAsync(Arg.Any<CancelBuildRequest>())
                .Returns(CreateAsyncUnaryCall(new Empty()));
            translationServiceClient.CreateAsync(Arg.Any<CreateRequest>()).Returns(CreateAsyncUnaryCall(new Empty()));
            translationServiceClient.DeleteAsync(Arg.Any<DeleteRequest>()).Returns(CreateAsyncUnaryCall(new Empty()));
            translationServiceClient
                .StartBuildAsync(Arg.Any<StartBuildRequest>())
                .Returns(CreateAsyncUnaryCall(new Empty()));
            translationServiceClient
                .TrainSegmentPairAsync(Arg.Any<TrainSegmentPairRequest>())
                .Returns(CreateAsyncUnaryCall(new Empty()));
            var grpcClientFactory = Substitute.For<GrpcClientFactory>();
            grpcClientFactory
                .CreateClient<TranslationService.TranslationServiceClient>("smt")
                .Returns(translationServiceClient);
            Service = new TranslationEngineService(Engines, new MemoryRepository<Build>(), grpcClientFactory);
        }

        public TranslationEngineService Service { get; }
        public IRepository<TranslationEngine> Engines { get; }

        public async Task<TranslationEngine> CreateEngineAsync()
        {
            var engine = new TranslationEngine
            {
                Id = "engine1",
                SourceLanguageTag = "es",
                TargetLanguageTag = "en",
                Type = "smt"
            };
            await Engines.InsertAsync(engine);
            return engine;
        }

        private static IEnumerable<uint> GetSources(int count, bool isUnknown)
        {
            var sources = new uint[count];
            for (int i = 0; i < count; i++)
                sources[i] = (uint)(isUnknown ? TranslationSources.None : TranslationSources.Smt);
            return sources;
        }

        private static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
        {
            return new AsyncUnaryCall<TResponse>(
                Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }
            );
        }

        protected override void DisposeManagedResources()
        {
            Service.Dispose();
        }
    }
}
