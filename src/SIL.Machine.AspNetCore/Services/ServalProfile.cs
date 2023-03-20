using Google.Protobuf.Collections;

namespace SIL.Machine.AspNetCore.Services;

public class ServalProfile : Profile
{
    public ServalProfile()
    {
        CreateMap(typeof(IEnumerable<>), typeof(RepeatedField<>))
            .ConvertUsing(typeof(EnumerableToRepeatedFieldTypeConverter<,>));
        CreateMap<WordAlignmentMatrix, RepeatedField<Serval.Translation.V1.AlignedWordPair>>()
            .ConvertUsing<WordAlignmentConverter>();
        CreateMap<(string Translation, TranslationResult Result), Serval.Translation.V1.TranslationResult>()
            .ForMember(dest => dest.Translation, o => o.MapFrom(src => src.Translation))
            .ForMember(dest => dest.Tokens, o => o.MapFrom(src => src.Result.TargetSegment))
            .ForMember(dest => dest.Confidences, o => o.MapFrom(src => src.Result.WordConfidences))
            .ForMember(dest => dest.Sources, o => o.MapFrom(src => src.Result.WordSources))
            .ForMember(dest => dest.Alignment, o => o.MapFrom(src => src.Result.Alignment))
            .ForMember(dest => dest.Phrases, o => o.MapFrom(src => src.Result.Phrases));
        CreateMap<Phrase, Serval.Translation.V1.Phrase>()
            .ForMember(dest => dest.SourceSegmentStart, o => o.MapFrom(src => src.SourceSegmentRange.Start))
            .ForMember(dest => dest.SourceSegmentEnd, o => o.MapFrom(src => src.SourceSegmentRange.End));
        CreateMap<WordGraph, Serval.Translation.V1.WordGraph>();
        CreateMap<WordGraphArc, Serval.Translation.V1.WordGraphArc>()
            .ForMember(dest => dest.Tokens, o => o.MapFrom(src => src.Words))
            .ForMember(dest => dest.Confidences, o => o.MapFrom(src => src.WordConfidences))
            .ForMember(dest => dest.SourceSegmentStart, o => o.MapFrom(src => src.SourceSegmentRange.Start))
            .ForMember(dest => dest.SourceSegmentEnd, o => o.MapFrom(src => src.SourceSegmentRange.End))
            .ForMember(dest => dest.Sources, o => o.MapFrom(src => src.WordSources));

        CreateMap<Serval.Translation.V1.ParallelCorpus, Corpus>();
        CreateMap<Serval.Translation.V1.ParallelCorpusFile, CorpusFile>();
    }
}

public class EnumerableToRepeatedFieldTypeConverter<TITemSource, TITemDest>
    : ITypeConverter<IEnumerable<TITemSource>, RepeatedField<TITemDest>>
{
    public RepeatedField<TITemDest> Convert(
        IEnumerable<TITemSource> source,
        RepeatedField<TITemDest>? destination,
        ResolutionContext context
    )
    {
        destination ??= new RepeatedField<TITemDest>();
        foreach (TITemSource item in source)
            destination.Add(context.Mapper.Map<TITemDest>(item));
        return destination;
    }
}

public class WordAlignmentConverter
    : ITypeConverter<WordAlignmentMatrix, RepeatedField<Serval.Translation.V1.AlignedWordPair>>
{
    public RepeatedField<Serval.Translation.V1.AlignedWordPair> Convert(
        WordAlignmentMatrix source,
        RepeatedField<Serval.Translation.V1.AlignedWordPair> destination,
        ResolutionContext context
    )
    {
        destination ??= new RepeatedField<Serval.Translation.V1.AlignedWordPair>();
        for (int i = 0; i < source.RowCount; i++)
        {
            for (int j = 0; j < source.ColumnCount; j++)
            {
                if (source[i, j])
                    destination.Add(new Serval.Translation.V1.AlignedWordPair { SourceIndex = i, TargetIndex = j });
            }
        }
        return destination;
    }
}
