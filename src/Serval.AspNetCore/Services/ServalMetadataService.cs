using Google.Protobuf.WellKnownTypes;
using Serval.Platform.Metadata.V1;

namespace Serval.AspNetCore.Services;

public class ServalMetadataService : MetadataService.MetadataServiceBase
{
    private static readonly Empty Empty = new();

    private readonly IRepository<TranslationEngine> _engines;
    private readonly IRepository<Models.Corpus> _corpora;

    public ServalMetadataService(IRepository<TranslationEngine> engines, IRepository<Models.Corpus> corpora)
    {
        _engines = engines;
        _corpora = corpora;
    }

    public override async Task<GetTranslationEngineResponse> GetTranslationEngine(
        GetTranslationEngineRequest request,
        ServerCallContext context
    )
    {
        TranslationEngine? engine = await _engines.GetAsync(request.EngineId, context.CancellationToken);
        if (engine is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The engine does not exist."));
        return new GetTranslationEngineResponse
        {
            EngineType = engine.Type,
            EngineId = engine.Id,
            Name = engine.Name,
            SourceLanguageTag = engine.SourceLanguageTag,
            TargetLanguageTag = engine.TargetLanguageTag
        };
    }

    public override async Task<GetParallelTextCorporaResponse> GetParallelTextCorpora(
        GetParallelTextCorporaRequest request,
        ServerCallContext context
    )
    {
        TranslationEngine? engine = await _engines.GetAsync(request.EngineId, context.CancellationToken);
        if (engine is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The engine does not exist."));

        var response = new GetParallelTextCorporaResponse();
        foreach (TranslationEngineCorpus corpus in engine.Corpora)
        {
            response.Corpora.Add(
                new ParallelTextCorpus
                {
                    CorpusId = corpus.CorpusRef,
                    Pretranslate = corpus.Pretranslate,
                    SourceCorpus = await CreateTextCorpusAsync(
                        corpus.CorpusRef,
                        engine.SourceLanguageTag,
                        context.CancellationToken
                    ),
                    TargetCorpus = await CreateTextCorpusAsync(
                        corpus.CorpusRef,
                        engine.TargetLanguageTag,
                        context.CancellationToken
                    )
                }
            );
        }
        return response;
    }

    public override async Task<Empty> IncrementTranslationEngineCorpusSize(
        IncrementTranslationEngineCorpusSizeRequest request,
        ServerCallContext context
    )
    {
        await _engines.UpdateAsync(
            request.EngineId,
            u => u.Inc(e => e.CorpusSize, request.Count),
            cancellationToken: context.CancellationToken
        );
        return Empty;
    }

    private async Task<Platform.Metadata.V1.Corpus?> CreateTextCorpusAsync(
        string corpusId,
        string languageTag,
        CancellationToken cancellationToken
    )
    {
        Models.Corpus? corpus = await _corpora.GetAsync(corpusId, cancellationToken);
        if (corpus is null || corpus.Type != Core.CorpusType.Text)
            return null;
        Models.DataFile[] files = corpus.Files.Where(f => f.LanguageTag == languageTag).ToArray();
        if (files.Length == 0)
            return null;

        var result = new Platform.Metadata.V1.Corpus
        {
            Name = corpus.Name,
            Format = (Platform.Metadata.V1.FileFormat)corpus.Format,
            Type = (Platform.Metadata.V1.CorpusType)corpus.Type
        };
        result.Files.Add(
            files.Select(
                f =>
                {
                    var dataFile = new Platform.Metadata.V1.DataFile
                    {
                        Name = f.Name,
                        Filename = f.Filename,
                        LanguageTag = f.LanguageTag
                    };
                    if (f.TextId is not null)
                        dataFile.TextId = f.TextId;
                    return dataFile;
                }
            )
        );
        return result;
    }
}
