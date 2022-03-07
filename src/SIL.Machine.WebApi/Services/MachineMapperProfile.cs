namespace SIL.Machine.WebApi.Services;

public class MachineMapperProfile : Profile
{
	private const string EnginesUrl = "/translation/engines";
	private const string WebhooksUrl = "/admin/hooks";

	public MachineMapperProfile()
	{
		CreateMap<Engine, EngineDto>()
			.ForMember(dto => dto.Href, o => o.MapFrom((e, _) => $"{EnginesUrl}/{e.Id}"));
		CreateMap<Build, BuildDto>()
			.ForMember(dto => dto.Href, o => o.MapFrom((b, _) => $"{EnginesUrl}/{b.EngineRef}/builds/{b.Id}"))
			.ForMember(dto => dto.Engine, o => o.MapFrom((b, _) =>
				new ResourceDto { Id = b.EngineRef, Href = $"{EnginesUrl}/{b.EngineRef}" }));
		CreateMap<DataFile, DataFileDto>()
			.ForMember(dto => dto.Href, o => o.MapFrom((f, _) => $"{EnginesUrl}/{f.EngineRef}/files/{f.Id}"))
			.ForMember(dto => dto.Engine, o => o.MapFrom((f, _) =>
				new ResourceDto { Id = f.EngineRef, Href = $"{EnginesUrl}/{f.EngineRef}" }));
		CreateMap<TranslationResult, TranslationResultDto>();
		CreateMap<WordAlignmentMatrix, AlignedWordPairDto[]>().ConvertUsing<WordAlignmentConverter>();
		CreateMap<Range<int>, RangeDto>();
		CreateMap<Phrase, PhraseDto>();
		CreateMap<WordGraph, WordGraphDto>();
		CreateMap<WordGraphArc, WordGraphArcDto>();
		CreateMap<Webhook, WebhookDto>()
			.ForMember(dto => dto.Href, o => o.MapFrom((h, _) => $"{WebhooksUrl}/{h.Id}"));
	}
}

public class WordAlignmentConverter : ITypeConverter<WordAlignmentMatrix, AlignedWordPairDto[]>
{
	public AlignedWordPairDto[] Convert(WordAlignmentMatrix source, AlignedWordPairDto[] destination,
		ResolutionContext context)
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
