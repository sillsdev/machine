namespace SIL.Machine.WebApi.Services;

public class MapperProfile : Profile
{
    private const string TranslationEnginesUrl = "/translation-engines";
    private const string WebhooksUrl = "/hooks";
    private const string CorporaUrl = "/corpora";

    public MapperProfile()
    {
        CreateMap<TranslationEngine, TranslationEngineDto>()
            .ForMember(dto => dto.Href, o => o.MapFrom((e, _) => $"{TranslationEnginesUrl}/{e.Id}"));
        CreateMap<TranslationEngineCorpus, TranslationEngineCorpusDto>()
            .ForMember(
                dto => dto.Corpus,
                o =>
                    o.MapFrom(
                        (tec, _) => new ResourceDto { Id = tec.CorpusRef, Href = $"{CorporaUrl}/{tec.CorpusRef}" }
                    )
            )
            .ForMember(
                dto => dto.Href,
                o =>
                    o.MapFrom(
                        (tec, _, _, ctxt) => $"{TranslationEnginesUrl}/{ctxt.Items["EngineId"]}/corpora/{tec.CorpusRef}"
                    )
            );
        CreateMap<Build, BuildDto>()
            .ForMember(
                dto => dto.Href,
                o => o.MapFrom((b, _) => $"{TranslationEnginesUrl}/{b.ParentRef}/builds/{b.Id}")
            )
            .ForMember(
                dto => dto.Parent,
                o =>
                    o.MapFrom(
                        (b, _) => new ResourceDto { Id = b.ParentRef, Href = $"{TranslationEnginesUrl}/{b.ParentRef}" }
                    )
            );
        CreateMap<Corpus, CorpusDto>().ForMember(dto => dto.Href, o => o.MapFrom((c, _) => $"{CorporaUrl}/{c.Id}"));
        CreateMap<DataFile, DataFileDto>()
            .ForMember(
                dto => dto.Href,
                o => o.MapFrom((f, _, _, ctxt) => $"{CorporaUrl}/{ctxt.Items["CorpusId"]}/files/{f.Id}")
            );
        CreateMap<TranslationResult, TranslationResultDto>()
            .ForMember(dto => dto.Target, o => o.MapFrom(r => r.TargetSegment))
            .ForMember(dto => dto.Confidences, o => o.MapFrom(r => r.WordConfidences))
            .ForMember(dto => dto.Sources, o => o.MapFrom(r => r.WordSources));
        CreateMap<TranslationResultDto, TranslationResult>()
            .ForCtorParam("sourceSegmentLength", o => o.MapFrom((_, ctxt) => ctxt.Items["SourceSegmentLength"]))
            .ForCtorParam("targetSegment", o => o.MapFrom(dto => dto.Target))
            .ForCtorParam(
                "alignment",
                o =>
                    o.MapFrom(
                        (dto, ctxt) =>
                            new WordAlignmentMatrix(
                                (int)ctxt.Items["SourceSegmentLength"],
                                dto.Target.Length,
                                dto.Alignment.Select(wp => (wp.SourceIndex, wp.TargetIndex))
                            )
                    )
            );
        CreateMap<WordAlignmentMatrix, AlignedWordPairDto[]>().ConvertUsing<WordAlignmentConverter>();
        CreateMap<Range<int>, RangeDto>().ReverseMap().ConvertUsing((dto, _) => Range<int>.Create(dto.Start, dto.End));
        CreateMap<Phrase, PhraseDto>().ReverseMap();
        CreateMap<WordGraph, WordGraphDto>().ReverseMap();
        CreateMap<WordGraphArc, WordGraphArcDto>()
            .ForMember(dto => dto.Confidences, o => o.MapFrom(a => a.WordConfidences))
            .ForMember(dto => dto.Sources, o => o.MapFrom(a => a.WordSources))
            .ReverseMap()
            .ForCtorParam(
                "alignment",
                o =>
                    o.MapFrom(
                        (dto, _) =>
                            new WordAlignmentMatrix(
                                dto.SourceSegmentRange.End - dto.SourceSegmentRange.Start,
                                dto.Words.Length,
                                dto.Alignment.Select(wp => (wp.SourceIndex, wp.TargetIndex))
                            )
                    )
            )
            .ForCtorParam("wordConfidences", o => o.MapFrom(dto => dto.Confidences))
            .ForCtorParam("wordSources", o => o.MapFrom(dto => dto.Sources));
        CreateMap<Webhook, WebhookDto>().ForMember(dto => dto.Href, o => o.MapFrom((h, _) => $"{WebhooksUrl}/{h.Id}"));
        CreateMap<Pretranslation, PretranslationDto>();
    }
}

public class WordAlignmentConverter : ITypeConverter<WordAlignmentMatrix, AlignedWordPairDto[]>
{
    public AlignedWordPairDto[] Convert(
        WordAlignmentMatrix source,
        AlignedWordPairDto[] destination,
        ResolutionContext context
    )
    {
        var wordPairs = new List<AlignedWordPairDto>();
        for (int i = 0; i < source.RowCount; i++)
        {
            for (int j = 0; j < source.ColumnCount; j++)
            {
                if (source[i, j])
                    wordPairs.Add(new AlignedWordPairDto { SourceIndex = i, TargetIndex = j });
            }
        }
        return wordPairs.ToArray();
    }
}
