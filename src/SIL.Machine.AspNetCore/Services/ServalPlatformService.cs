using Serval.Platform.BuildStatus.V1;
using Serval.Platform.Metadata.V1;
using Serval.Platform.Result.V1;

namespace SIL.Machine.AspNetCore.Services;

public class ServalPlatformService : IPlatformService
{
    private readonly BuildStatusService.BuildStatusServiceClient _buildStatusServiceClient;
    private readonly MetadataService.MetadataServiceClient _metadataServiceClient;
    private readonly ResultService.ResultServiceClient _resultServiceClient;
    private readonly IOptionsMonitor<ServalOptions> _options;

    public ServalPlatformService(
        BuildStatusService.BuildStatusServiceClient buildStatusServiceClient,
        MetadataService.MetadataServiceClient metadataServiceClient,
        ResultService.ResultServiceClient resultServiceClient,
        IOptionsMonitor<ServalOptions> options
    )
    {
        _buildStatusServiceClient = buildStatusServiceClient;
        _metadataServiceClient = metadataServiceClient;
        _resultServiceClient = resultServiceClient;
        _options = options;
    }

    public async Task BuildStartedAsync(string buildId, CancellationToken cancellationToken = default)
    {
        await _buildStatusServiceClient.BuildStartedAsync(
            new BuildStartedRequest { BuildId = buildId },
            cancellationToken: cancellationToken
        );
    }

    public async Task BuildCompletedAsync(
        string buildId,
        int trainSize,
        double confidence,
        CancellationToken cancellationToken = default
    )
    {
        await _buildStatusServiceClient.BuildCompletedAsync(
            new BuildCompletedRequest
            {
                BuildId = buildId,
                CorpusSize = trainSize,
                Confidence = confidence
            },
            cancellationToken: cancellationToken
        );
    }

    public async Task BuildCanceledAsync(string buildId, CancellationToken cancellationToken = default)
    {
        await _buildStatusServiceClient.BuildCanceledAsync(
            new BuildCanceledRequest { BuildId = buildId },
            cancellationToken: cancellationToken
        );
    }

    public async Task BuildFaultedAsync(string buildId, string message, CancellationToken cancellationToken = default)
    {
        await _buildStatusServiceClient.BuildFaultedAsync(
            new BuildFaultedRequest { BuildId = buildId, Message = message },
            cancellationToken: cancellationToken
        );
    }

    public async Task BuildRestartingAsync(string buildId, CancellationToken cancellationToken = default)
    {
        await _buildStatusServiceClient.BuildRestartingAsync(
            new BuildRestartingRequest { BuildId = buildId },
            cancellationToken: cancellationToken
        );
    }

    public async Task UpdateBuildStatusAsync(
        string buildId,
        ProgressStatus progressStatus,
        CancellationToken cancellationToken = default
    )
    {
        var request = new UpdateBuildStatusRequest { BuildId = buildId, Step = progressStatus.Step };
        if (progressStatus.PercentCompleted.HasValue)
            request.PercentCompleted = progressStatus.PercentCompleted.Value;
        if (progressStatus.Message is not null)
            request.Message = progressStatus.Message;

        await _buildStatusServiceClient.UpdateBuildStatusAsync(request, cancellationToken: cancellationToken);
    }

    public async Task UpdateBuildStatusAsync(string buildId, int step, CancellationToken cancellationToken = default)
    {
        await _buildStatusServiceClient.UpdateBuildStatusAsync(
            new UpdateBuildStatusRequest { BuildId = buildId, Step = step },
            cancellationToken: cancellationToken
        );
    }

    public async IAsyncEnumerable<CorpusInfo> GetCorporaAsync(
        string engineId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        GetParallelTextCorporaResponse response = await _metadataServiceClient.GetParallelTextCorporaAsync(
            new GetParallelTextCorporaRequest { EngineId = engineId },
            cancellationToken: cancellationToken
        );
        foreach (Serval.Platform.Metadata.V1.ParallelTextCorpus corpus in response.Corpora)
        {
            ITextCorpus? sc = CreateTextCorpus(corpus.SourceCorpus);
            ITextCorpus? tc = CreateTextCorpus(corpus.TargetCorpus);
            yield return new(corpus.CorpusId, corpus.Pretranslate, sc, tc);
        }
    }

    public async Task<string?> GetEngineName(string engineId, CancellationToken cancellationToken = default)
    {
        GetTranslationEngineResponse response = await _metadataServiceClient.GetTranslationEngineAsync(
            new GetTranslationEngineRequest { EngineId = engineId },
            cancellationToken: cancellationToken
        );
        return response.Name;
    }

    public async Task DeleteAllPretranslationsAsync(string engineId, CancellationToken cancellationToken = default)
    {
        await _resultServiceClient.DeleteAllPretranslationsAsync(
            new DeleteAllPretranslationsRequest { EngineId = engineId },
            cancellationToken: cancellationToken
        );
    }

    public async Task InsertPretranslationsAsync(
        string engineId,
        IAsyncEnumerable<PretranslationInfo> pretranslations,
        CancellationToken cancellationToken = default
    )
    {
        using var call = _resultServiceClient.InsertPretranslations(cancellationToken: cancellationToken);
        await foreach (PretranslationInfo? pretranslation in pretranslations)
        {
            await call.RequestStream.WriteAsync(
                new InsertPretranslationRequest
                {
                    EngineId = engineId,
                    CorpusId = pretranslation.CorpusId,
                    TextId = pretranslation.TextId,
                    Translation = pretranslation.Translation
                },
                cancellationToken
            );
        }
        await call.RequestStream.CompleteAsync();
    }

    public async Task<TranslationEngineInfo> GetTranslationEngineInfoAsync(
        string engineId,
        CancellationToken cancellationToken = default
    )
    {
        GetTranslationEngineResponse response = await _metadataServiceClient.GetTranslationEngineAsync(
            new GetTranslationEngineRequest { EngineId = engineId },
            cancellationToken: cancellationToken
        );
        return new(
            response.EngineType,
            response.EngineId,
            response.Name,
            response.SourceLanguageTag,
            response.TargetLanguageTag
        );
    }

    public async Task IncrementTrainSizeAsync(
        string engineId,
        int count = 1,
        CancellationToken cancellationToken = default
    )
    {
        await _metadataServiceClient.IncrementTranslationEngineCorpusSizeAsync(
            new IncrementTranslationEngineCorpusSizeRequest { EngineId = engineId, Count = count },
            cancellationToken: cancellationToken
        );
    }

    private ITextCorpus? CreateTextCorpus(Corpus corpus)
    {
        if (corpus.Files.Count == 0)
            return null;

        ITextCorpus? textCorpus = null;
        switch (corpus.Format)
        {
            case FileFormat.Text:
                textCorpus = new DictionaryTextCorpus(
                    corpus.Files.Select(f => new TextFileText(f.TextId ?? f.Name, GetDataFilePath(f)))
                );
                break;

            case FileFormat.Paratext:
                textCorpus = new ParatextBackupTextCorpus(GetDataFilePath(corpus.Files[0]));
                break;
        }
        return textCorpus;
    }

    private string GetDataFilePath(DataFile dataFile)
    {
        return Path.Combine(_options.CurrentValue.DataFilesDir, dataFile.Filename);
    }
}
