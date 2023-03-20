using Google.Protobuf.WellKnownTypes;
using Serval.Translation.V1;

namespace SIL.Machine.AspNetCore.Services;

public class ServalTranslationEngineServiceV1 : TranslationEngineApi.TranslationEngineApiBase
{
    private static readonly Empty Empty = new();

    private readonly Dictionary<TranslationEngineType, ITranslationEngineService> _engineServices;
    private readonly IMapper _mapper;

    public ServalTranslationEngineServiceV1(IEnumerable<ITranslationEngineService> engineServices, IMapper mapper)
    {
        _engineServices = engineServices.ToDictionary(es => es.Type);
        _mapper = mapper;
    }

    public override async Task<Empty> Create(CreateRequest request, ServerCallContext context)
    {
        ITranslationEngineService engineService = GetEngineService(request.EngineType);
        await engineService.CreateAsync(
            request.EngineId,
            request.SourceLanguage,
            request.TargetLanguage,
            context.CancellationToken
        );
        return Empty;
    }

    public override async Task<Empty> Delete(DeleteRequest request, ServerCallContext context)
    {
        ITranslationEngineService engineService = GetEngineService(request.EngineType);
        await engineService.DeleteAsync(request.EngineId, context.CancellationToken);
        return Empty;
    }

    public override async Task<TranslateResponse> Translate(TranslateRequest request, ServerCallContext context)
    {
        ITranslationEngineService engineService = GetEngineService(request.EngineType);
        IEnumerable<(string, Translation.TranslationResult)> results = await engineService.TranslateAsync(
            request.EngineId,
            request.N,
            request.Segment,
            context.CancellationToken
        );
        return new TranslateResponse
        {
            Results = { results.Select(r => _mapper.Map<Serval.Translation.V1.TranslationResult>(r)) }
        };
    }

    public override async Task<GetWordGraphResponse> GetWordGraph(
        GetWordGraphRequest request,
        ServerCallContext context
    )
    {
        ITranslationEngineService engineService = GetEngineService(request.EngineType);
        Translation.WordGraph wordGraph = await engineService.GetWordGraphAsync(
            request.EngineId,
            request.Segment,
            context.CancellationToken
        );
        return new GetWordGraphResponse { WordGraph = _mapper.Map<Serval.Translation.V1.WordGraph>(wordGraph) };
    }

    public override async Task<Empty> TrainSegmentPair(TrainSegmentPairRequest request, ServerCallContext context)
    {
        ITranslationEngineService engineService = GetEngineService(request.EngineType);
        await engineService.TrainSegmentPairAsync(
            request.EngineId,
            request.SourceSegment,
            request.TargetSegment,
            request.SentenceStart,
            context.CancellationToken
        );
        return Empty;
    }

    public override async Task<Empty> StartBuild(StartBuildRequest request, ServerCallContext context)
    {
        ITranslationEngineService engineService = GetEngineService(request.EngineType);
        Corpus[] corpora = request.Corpora.Select(_mapper.Map<Corpus>).ToArray();
        await engineService.StartBuildAsync(request.EngineId, request.BuildId, corpora, context.CancellationToken);
        return Empty;
    }

    public override async Task<Empty> CancelBuild(CancelBuildRequest request, ServerCallContext context)
    {
        ITranslationEngineService engineService = GetEngineService(request.EngineType);
        await engineService.CancelBuildAsync(request.EngineId, context.CancellationToken);
        return Empty;
    }

    private ITranslationEngineService GetEngineService(string engineTypeStr)
    {
        if (_engineServices.TryGetValue(GetEngineType(engineTypeStr), out ITranslationEngineService? service))
            return service;
        throw new RpcException(new Status(StatusCode.InvalidArgument, "The engine type is invalid."));
    }

    private static TranslationEngineType GetEngineType(string engineTypeStr)
    {
        engineTypeStr = engineTypeStr[0].ToString().ToUpperInvariant() + engineTypeStr[1..];
        if (System.Enum.TryParse(engineTypeStr, out TranslationEngineType engineType))
            return engineType;
        throw new RpcException(new Status(StatusCode.InvalidArgument, "The engine type is invalid."));
    }
}
