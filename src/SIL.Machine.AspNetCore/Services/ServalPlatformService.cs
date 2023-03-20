using Serval.Translation.V1;

namespace SIL.Machine.AspNetCore.Services;

public class ServalPlatformService : IPlatformService
{
    private readonly TranslationPlatformApi.TranslationPlatformApiClient _client;

    public ServalPlatformService(TranslationPlatformApi.TranslationPlatformApiClient client)
    {
        _client = client;
    }

    public async Task BuildStartedAsync(string buildId, CancellationToken cancellationToken = default)
    {
        await _client.BuildStartedAsync(
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
        await _client.BuildCompletedAsync(
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
        await _client.BuildCanceledAsync(
            new BuildCanceledRequest { BuildId = buildId },
            cancellationToken: cancellationToken
        );
    }

    public async Task BuildFaultedAsync(string buildId, string message, CancellationToken cancellationToken = default)
    {
        await _client.BuildFaultedAsync(
            new BuildFaultedRequest { BuildId = buildId, Message = message },
            cancellationToken: cancellationToken
        );
    }

    public async Task BuildRestartingAsync(string buildId, CancellationToken cancellationToken = default)
    {
        await _client.BuildRestartingAsync(
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

        await _client.UpdateBuildStatusAsync(request, cancellationToken: cancellationToken);
    }

    public async Task UpdateBuildStatusAsync(string buildId, int step, CancellationToken cancellationToken = default)
    {
        await _client.UpdateBuildStatusAsync(
            new UpdateBuildStatusRequest { BuildId = buildId, Step = step },
            cancellationToken: cancellationToken
        );
    }

    public async Task DeleteAllPretranslationsAsync(string engineId, CancellationToken cancellationToken = default)
    {
        await _client.DeleteAllPretranslationsAsync(
            new DeleteAllPretranslationsRequest { EngineId = engineId },
            cancellationToken: cancellationToken
        );
    }

    public async Task InsertPretranslationsAsync(
        string engineId,
        IAsyncEnumerable<Pretranslation> pretranslations,
        CancellationToken cancellationToken = default
    )
    {
        using var call = _client.InsertPretranslations(cancellationToken: cancellationToken);
        await foreach (Pretranslation? pretranslation in pretranslations)
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
        await call;
    }

    public async Task IncrementTrainSizeAsync(
        string engineId,
        int count = 1,
        CancellationToken cancellationToken = default
    )
    {
        await _client.IncrementTranslationEngineCorpusSizeAsync(
            new IncrementTranslationEngineCorpusSizeRequest { EngineId = engineId, Count = count },
            cancellationToken: cancellationToken
        );
    }
}
