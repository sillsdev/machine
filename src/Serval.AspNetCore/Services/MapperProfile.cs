using Serval.Core;
using Serval.Engine.Translation.V1;

namespace Serval.AspNetCore.Services;

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
        CreateMap<TranslationResult, TranslationResultDto>();
        CreateMap<AlignedWordPair, AlignedWordPairDto>();
        CreateMap<Phrase, PhraseDto>();
        CreateMap<WordGraph, WordGraphDto>();
        CreateMap<WordGraphArc, WordGraphArcDto>();
        CreateMap<Webhook, WebhookDto>().ForMember(dto => dto.Href, o => o.MapFrom((h, _) => $"{WebhooksUrl}/{h.Id}"));
        CreateMap<Pretranslation, PretranslationDto>();
    }
}
