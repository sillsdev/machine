namespace SIL.Machine.WebApi.Services;

public class RemoteTranslationEngineRuntimeService : ITranslationEngineRuntimeService
{
    private readonly IRepository<Build> _builds;
    private readonly IGrpcTranslationEngineService _grpcTranslationEngineService;
    private readonly IMapper _mapper;

    public RemoteTranslationEngineRuntimeService(
        IRepository<Build> builds,
        IGrpcTranslationEngineService grpcTranslationEngineService,
        IMapper mapper
    )
    {
        _builds = builds;
        _grpcTranslationEngineService = grpcTranslationEngineService;
        _mapper = mapper;
    }

    public void Init() { }

    public Task CreateAsync(TranslationEngineType engineType, string engineId)
    {
        return _grpcTranslationEngineService.CreateAsync(
            new TranslationEngineCreateRequest { EngineType = engineType, EngineId = engineId }
        );
    }

    public Task DeleteAsync(TranslationEngineType engineType, string engineId)
    {
        return _grpcTranslationEngineService.DeleteAsync(
            new TranslationEngineDeleteRequest { EngineType = engineType, EngineId = engineId }
        );
    }

    public async Task<TranslationResult> TranslateAsync(
        TranslationEngineType engineType,
        string engineId,
        IReadOnlyList<string> segment
    )
    {
        TranslationEngineTranslateResponse response = await _grpcTranslationEngineService.TranslateAsync(
            new TranslationEngineTranslateRequest
            {
                EngineType = engineType,
                EngineId = engineId,
                Segment = segment
            }
        );
        return _mapper.Map<TranslationResult>(
            response.Results.First(),
            o => o.Items["SourceSegmentLength"] = segment.Count
        );
    }

    public async Task<IEnumerable<TranslationResult>> TranslateAsync(
        TranslationEngineType engineType,
        string engineId,
        int n,
        IReadOnlyList<string> segment
    )
    {
        TranslationEngineTranslateResponse response = await _grpcTranslationEngineService.TranslateAsync(
            new TranslationEngineTranslateRequest
            {
                EngineType = engineType,
                EngineId = engineId,
                N = n,
                Segment = segment
            }
        );
        return response.Results.Select(
            tr => _mapper.Map<TranslationResult>(tr, o => o.Items["SourceSegmentLength"] = segment.Count)
        );
    }

    public async Task<WordGraph> GetWordGraphAsync(
        TranslationEngineType engineType,
        string engineId,
        IReadOnlyList<string> segment
    )
    {
        TranslationEngineGetWordGraphResponse response = await _grpcTranslationEngineService.GetWordGraphAsync(
            new TranslationEngineGetWordGraphRequest
            {
                EngineType = engineType,
                EngineId = engineId,
                Segment = segment
            }
        );
        return _mapper.Map<WordGraph>(response.WordGraph);
    }

    public Task TrainSegmentPairAsync(
        TranslationEngineType engineType,
        string engineId,
        IReadOnlyList<string> sourceSegment,
        IReadOnlyList<string> targetSegment,
        bool sentenceStart
    )
    {
        return _grpcTranslationEngineService.TrainSegmentPairAsync(
            new TranslationEngineTrainSegmentPairRequest
            {
                EngineType = engineType,
                EngineId = engineId,
                SourceSegment = sourceSegment,
                TargetSegment = targetSegment,
                SentenceStart = sentenceStart
            }
        );
    }

    public async Task<Build> StartBuildAsync(TranslationEngineType engineType, string engineId)
    {
        TranslationEngineStartBuildResponse response = await _grpcTranslationEngineService.StartBuildAsync(
            new TranslationEngineStartBuildRequest { EngineType = engineType, EngineId = engineId }
        );
        return (await _builds.GetAsync(response.BuildId))!;
    }

    public Task CancelBuildAsync(string engineId)
    {
        return _grpcTranslationEngineService.CancelBuildAsync(
            new TranslationEngineCancelBuildRequest { EngineId = engineId }
        );
    }
}
