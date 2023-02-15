using Google.Protobuf.WellKnownTypes;
using Serval.Engine.Translation.V1;

namespace SIL.Machine.AspNetCore.Services;

public class ServalTranslationService : TranslationService.TranslationServiceBase
{
    private static readonly Empty Empty = new();

    private readonly ITranslationEngineRuntimeService _engineRuntimeService;

    public ServalTranslationService(ITranslationEngineRuntimeService engineRuntimeService)
    {
        _engineRuntimeService = engineRuntimeService;
    }

    public override async Task<Empty> Create(CreateRequest request, ServerCallContext context)
    {
        await _engineRuntimeService.CreateAsync(GetEngineType(request.EngineType), request.EngineId);
        return Empty;
    }

    public override async Task<Empty> Delete(DeleteRequest request, ServerCallContext context)
    {
        await _engineRuntimeService.DeleteAsync(GetEngineType(request.EngineType), request.EngineId);
        return Empty;
    }

    public override async Task<TranslateResponse> Translate(TranslateRequest request, ServerCallContext context)
    {
        IEnumerable<(string, Translation.TranslationResult)> results = await _engineRuntimeService.TranslateAsync(
            GetEngineType(request.EngineType),
            request.EngineId,
            request.N,
            request.Segment
        );
        return new TranslateResponse { Results = { results.Select(Map) } };
    }

    public override async Task<GetWordGraphResponse> GetWordGraph(
        GetWordGraphRequest request,
        ServerCallContext context
    )
    {
        Translation.WordGraph wordGraph = await _engineRuntimeService.GetWordGraphAsync(
            GetEngineType(request.EngineType),
            request.EngineId,
            request.Segment
        );
        return new GetWordGraphResponse { WordGraph = Map(wordGraph) };
    }

    public override async Task<Empty> TrainSegmentPair(TrainSegmentPairRequest request, ServerCallContext context)
    {
        await _engineRuntimeService.TrainSegmentPairAsync(
            GetEngineType(request.EngineType),
            request.EngineId,
            request.SourceSegment,
            request.TargetSegment,
            request.SentenceStart
        );
        return Empty;
    }

    public override async Task<Empty> StartBuild(StartBuildRequest request, ServerCallContext context)
    {
        await _engineRuntimeService.StartBuildAsync(
            GetEngineType(request.EngineType),
            request.EngineId,
            request.BuildId
        );
        return Empty;
    }

    public override async Task<Empty> CancelBuild(CancelBuildRequest request, ServerCallContext context)
    {
        await _engineRuntimeService.CancelBuildAsync(request.EngineId);
        return Empty;
    }

    private static Serval.Engine.Translation.V1.TranslationResult Map(
        (string Translation, Translation.TranslationResult Result) source
    )
    {
        return new Serval.Engine.Translation.V1.TranslationResult
        {
            Translation = source.Translation,
            Tokens = { source.Result.TargetSegment },
            Confidences = { source.Result.WordConfidences },
            Sources = { source.Result.WordSources.Select(s => (uint)s) },
            AlignedWordPairs = { Map(source.Result.Alignment) },
            Phrases = { source.Result.Phrases.Select(Map) }
        };
    }

    private static IEnumerable<Serval.Engine.Translation.V1.AlignedWordPair> Map(WordAlignmentMatrix source)
    {
        var wordPairs = new List<Serval.Engine.Translation.V1.AlignedWordPair>();
        for (int i = 0; i < source.RowCount; i++)
        {
            for (int j = 0; j < source.ColumnCount; j++)
            {
                if (source[i, j])
                    wordPairs.Add(
                        new Serval.Engine.Translation.V1.AlignedWordPair { SourceIndex = i, TargetIndex = j }
                    );
            }
        }
        return wordPairs;
    }

    private static Serval.Engine.Translation.V1.Phrase Map(Translation.Phrase source)
    {
        return new Serval.Engine.Translation.V1.Phrase
        {
            SourceSegmentStart = source.SourceSegmentRange.Start,
            SourceSegmentEnd = source.SourceSegmentRange.End,
            TargetSegmentCut = source.TargetSegmentCut,
            Confidence = source.Confidence
        };
    }

    private static Serval.Engine.Translation.V1.WordGraph Map(Translation.WordGraph source)
    {
        return new Serval.Engine.Translation.V1.WordGraph
        {
            InitialStateScore = source.InitialStateScore,
            FinalStates = { source.FinalStates },
            Arcs = { source.Arcs.Select(Map) }
        };
    }

    private static Serval.Engine.Translation.V1.WordGraphArc Map(Translation.WordGraphArc source)
    {
        return new Serval.Engine.Translation.V1.WordGraphArc
        {
            PrevState = source.PrevState,
            NextState = source.NextState,
            Score = source.Score,
            Tokens = { source.Words },
            Confidences = { source.WordConfidences },
            SourceSegmentStart = source.SourceSegmentRange.Start,
            SourceSegmentEnd = source.SourceSegmentRange.End,
            Sources = { source.WordSources.Select(s => (uint)s) }
        };
    }

    private static TranslationEngineType GetEngineType(string engineTypeStr)
    {
        engineTypeStr = engineTypeStr[0].ToString().ToUpperInvariant() + engineTypeStr[1..];
        if (System.Enum.TryParse(engineTypeStr, out TranslationEngineType engineType))
            return engineType;
        throw new RpcException(new Status(StatusCode.InvalidArgument, "The engine type is invalid."));
    }
}
