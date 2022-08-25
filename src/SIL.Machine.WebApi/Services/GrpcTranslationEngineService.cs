namespace SIL.Machine.WebApi.Services;

public class GrpcTranslationEngineService : IGrpcTranslationEngineService
{
    private readonly ITranslationEngineRuntimeService _engineRuntimeService;
    private readonly IMapper _mapper;

    public GrpcTranslationEngineService(ITranslationEngineRuntimeService engineRuntimeService, IMapper mapper)
    {
        _engineRuntimeService = engineRuntimeService;
        _mapper = mapper;
    }

    public Task CreateAsync(TranslationEngineCreateRequest request)
    {
        return _engineRuntimeService.CreateAsync(request.EngineType, request.EngineId);
    }

    public Task DeleteAsync(TranslationEngineDeleteRequest request)
    {
        return _engineRuntimeService.DeleteAsync(request.EngineType, request.EngineId);
    }

    public async Task<TranslationEngineTranslateResponse> TranslateAsync(TranslationEngineTranslateRequest request)
    {
        IEnumerable<TranslationResult> results = await _engineRuntimeService.TranslateAsync(
            request.EngineType,
            request.EngineId,
            request.N,
            request.Segment
        );
        return new TranslationEngineTranslateResponse
        {
            Results = results.Select(_mapper.Map<TranslationResultDto>).ToList()
        };
    }

    public async Task<TranslationEngineGetWordGraphResponse> GetWordGraphAsync(
        TranslationEngineGetWordGraphRequest request
    )
    {
        WordGraph wordGraph = await _engineRuntimeService.GetWordGraphAsync(
            request.EngineType,
            request.EngineId,
            request.Segment
        );
        return new TranslationEngineGetWordGraphResponse { WordGraph = _mapper.Map<WordGraphDto>(wordGraph) };
    }

    public Task TrainSegmentPairAsync(TranslationEngineTrainSegmentPairRequest request)
    {
        return _engineRuntimeService.TrainSegmentPairAsync(
            request.EngineType,
            request.EngineId,
            request.SourceSegment,
            request.TargetSegment,
            request.SentenceStart
        );
    }

    public async Task<TranslationEngineStartBuildResponse> StartBuildAsync(TranslationEngineStartBuildRequest request)
    {
        Build build = await _engineRuntimeService.StartBuildAsync(request.EngineType, request.EngineId);
        return new TranslationEngineStartBuildResponse { BuildId = build.Id };
    }

    public Task CancelBuildAsync(TranslationEngineCancelBuildRequest request)
    {
        return _engineRuntimeService.CancelBuildAsync(request.EngineId);
    }
}
