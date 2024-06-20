using Serval.Translation.V1;

namespace SIL.Machine.AspNetCore.Services;

public class ServalPlatformService(
    TranslationPlatformApi.TranslationPlatformApiClient client,
    IMessageOutboxService outboxService
) : IPlatformService
{
    private readonly TranslationPlatformApi.TranslationPlatformApiClient _client = client;
    private readonly IMessageOutboxService _outboxService = outboxService;

    public async Task BuildStartedAsync(string buildId, CancellationToken cancellationToken = default)
    {
        await _outboxService.EnqueueMessageAsync(
            ServalPlatformMessageMethod.BuildStarted,
            buildId,
            JsonSerializer.Serialize(new BuildStartedRequest { BuildId = buildId }),
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
        await _outboxService.EnqueueMessageAsync(
            ServalPlatformMessageMethod.BuildCompleted,
            buildId,
            JsonSerializer.Serialize(
                new BuildCompletedRequest
                {
                    BuildId = buildId,
                    CorpusSize = trainSize,
                    Confidence = confidence
                }
            ),
            cancellationToken: cancellationToken
        );
    }

    public async Task BuildCanceledAsync(string buildId, CancellationToken cancellationToken = default)
    {
        await _outboxService.EnqueueMessageAsync(
            ServalPlatformMessageMethod.BuildCanceled,
            buildId,
            JsonSerializer.Serialize(new BuildCanceledRequest { BuildId = buildId }),
            cancellationToken: cancellationToken
        );
    }

    public async Task BuildFaultedAsync(string buildId, string message, CancellationToken cancellationToken = default)
    {
        await _outboxService.EnqueueMessageAsync(
            ServalPlatformMessageMethod.BuildFaulted,
            buildId,
            JsonSerializer.Serialize(new BuildFaultedRequest { BuildId = buildId, Message = message }),
            cancellationToken: cancellationToken
        );
    }

    public async Task BuildRestartingAsync(string buildId, CancellationToken cancellationToken = default)
    {
        await _outboxService.EnqueueMessageAsync(
            ServalPlatformMessageMethod.BuildRestarting,
            buildId,
            JsonSerializer.Serialize(new BuildRestartingRequest { BuildId = buildId }),
            cancellationToken: cancellationToken
        );
    }

    public async Task UpdateBuildStatusAsync(
        string buildId,
        ProgressStatus progressStatus,
        int? queueDepth = null,
        CancellationToken cancellationToken = default
    )
    {
        var request = new UpdateBuildStatusRequest { BuildId = buildId, Step = progressStatus.Step };
        if (progressStatus.PercentCompleted.HasValue)
            request.PercentCompleted = progressStatus.PercentCompleted.Value;
        if (progressStatus.Message is not null)
            request.Message = progressStatus.Message;
        if (queueDepth is not null)
            request.QueueDepth = queueDepth.Value;

        // just try to send it - if it fails, it fails.
        await _client.UpdateBuildStatusAsync(request, cancellationToken: cancellationToken);
    }

    public async Task UpdateBuildStatusAsync(string buildId, int step, CancellationToken cancellationToken = default)
    {
        // just try to send it - if it fails, it fails.
        await _client.UpdateBuildStatusAsync(
            new UpdateBuildStatusRequest { BuildId = buildId, Step = step },
            cancellationToken: cancellationToken
        );
    }

    public async Task InsertPretranslationsAsync(
        string engineId,
        string path,
        CancellationToken cancellationToken = default
    )
    {
        await _outboxService.EnqueueMessageAsync(
            ServalPlatformMessageMethod.InsertPretranslations,
            engineId,
            requestContent: engineId,
            requestContentPath: path,
            cancellationToken: cancellationToken
        );
    }

    public async Task IncrementTrainSizeAsync(
        string engineId,
        int count = 1,
        CancellationToken cancellationToken = default
    )
    {
        await _outboxService.EnqueueMessageAsync(
            ServalPlatformMessageMethod.IncrementTranslationEngineCorpusSize,
            engineId,
            JsonSerializer.Serialize(
                new IncrementTranslationEngineCorpusSizeRequest { EngineId = engineId, Count = count }
            ),
            cancellationToken: cancellationToken
        );
    }
}
