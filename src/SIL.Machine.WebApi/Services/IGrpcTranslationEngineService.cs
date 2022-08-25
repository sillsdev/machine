namespace SIL.Machine.WebApi.Services;

[DataContract]
public class TranslationEngineCreateRequest
{
    [DataMember(Order = 1)]
    public TranslationEngineType EngineType { get; set; }

    [DataMember(Order = 2)]
    public string EngineId { get; set; } = string.Empty;
}

[DataContract]
public class TranslationEngineDeleteRequest
{
    [DataMember(Order = 1)]
    public TranslationEngineType EngineType { get; set; }

    [DataMember(Order = 2)]
    public string EngineId { get; set; } = string.Empty;
}

[DataContract]
public class TranslationEngineTranslateRequest
{
    [DataMember(Order = 1)]
    public TranslationEngineType EngineType { get; set; }

    [DataMember(Order = 2)]
    public string EngineId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public IReadOnlyList<string> Segment { get; set; } = new List<string>();

    [DataMember(Order = 4)]
    public int N { get; set; } = 1;
}

[DataContract]
public class TranslationEngineTranslateResponse
{
    [DataMember(Order = 1)]
    public IReadOnlyList<TranslationResultDto> Results { get; set; } = new List<TranslationResultDto>();
}

[DataContract]
public class TranslationEngineGetWordGraphRequest
{
    [DataMember(Order = 1)]
    public TranslationEngineType EngineType { get; set; }

    [DataMember(Order = 2)]
    public string EngineId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public IReadOnlyList<string> Segment { get; set; } = new List<string>();
}

[DataContract]
public class TranslationEngineGetWordGraphResponse
{
    [DataMember(Order = 1)]
    public WordGraphDto WordGraph { get; set; } = new WordGraphDto();
}

[DataContract]
public class TranslationEngineTrainSegmentPairRequest
{
    [DataMember(Order = 1)]
    public TranslationEngineType EngineType { get; set; }

    [DataMember(Order = 2)]
    public string EngineId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public IReadOnlyList<string> SourceSegment { get; set; } = new List<string>();

    [DataMember(Order = 4)]
    public IReadOnlyList<string> TargetSegment { get; set; } = new List<string>();

    [DataMember(Order = 5)]
    public bool SentenceStart { get; set; }
}

[DataContract]
public class TranslationEngineStartBuildRequest
{
    [DataMember(Order = 1)]
    public TranslationEngineType EngineType { get; set; }

    [DataMember(Order = 2)]
    public string EngineId { get; set; } = string.Empty;
}

[DataContract]
public class TranslationEngineStartBuildResponse
{
    [DataMember(Order = 1)]
    public string BuildId { get; set; } = string.Empty;
}

[DataContract]
public class TranslationEngineCancelBuildRequest
{
    [DataMember(Order = 1)]
    public string EngineId { get; set; } = string.Empty;
}

[ServiceContract(Name = "machine.TranslationEngine")]
public interface IGrpcTranslationEngineService
{
    Task CreateAsync(TranslationEngineCreateRequest request);
    Task DeleteAsync(TranslationEngineDeleteRequest request);
    Task<TranslationEngineTranslateResponse> TranslateAsync(TranslationEngineTranslateRequest request);
    Task<TranslationEngineGetWordGraphResponse> GetWordGraphAsync(TranslationEngineGetWordGraphRequest request);
    Task TrainSegmentPairAsync(TranslationEngineTrainSegmentPairRequest request);
    Task<TranslationEngineStartBuildResponse> StartBuildAsync(TranslationEngineStartBuildRequest request);
    Task CancelBuildAsync(TranslationEngineCancelBuildRequest request);
}
