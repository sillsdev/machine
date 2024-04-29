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
            OutboxMessageMethod.BuildStarted,
            buildId,
            JsonSerializer.Serialize(new BuildStartedRequest { BuildId = buildId }),
            cancellationToken
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
            OutboxMessageMethod.BuildCompleted,
            buildId,
            JsonSerializer.Serialize(
                new BuildCompletedRequest
                {
                    BuildId = buildId,
                    CorpusSize = trainSize,
                    Confidence = confidence
                }
            ),
            cancellationToken
        );
    }

    public async Task BuildCanceledAsync(string buildId, CancellationToken cancellationToken = default)
    {
        await _outboxService.EnqueueMessageAsync(
            OutboxMessageMethod.BuildCanceled,
            buildId,
            JsonSerializer.Serialize(new BuildCanceledRequest { BuildId = buildId }),
            cancellationToken
        );
    }

    public async Task BuildFaultedAsync(string buildId, string message, CancellationToken cancellationToken = default)
    {
        await _outboxService.EnqueueMessageAsync(
            OutboxMessageMethod.BuildFaulted,
            buildId,
            JsonSerializer.Serialize(new BuildFaultedRequest { BuildId = buildId, Message = message }),
            cancellationToken
        );
    }

    public async Task BuildRestartingAsync(string buildId, CancellationToken cancellationToken = default)
    {
        await _outboxService.EnqueueMessageAsync(
            OutboxMessageMethod.BuildRestarting,
            buildId,
            JsonSerializer.Serialize(new BuildRestartingRequest { BuildId = buildId }),
            cancellationToken
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
        IAsyncEnumerable<Pretranslation> pretranslations,
        CancellationToken cancellationToken = default
    )
    {
        IList<InsertPretranslationRequest> requests = new List<InsertPretranslationRequest>();
        await foreach (Pretranslation? pretranslation in pretranslations)
        {
            requests.Add(
                new InsertPretranslationRequest
                {
                    EngineId = engineId,
                    CorpusId = pretranslation.CorpusId,
                    TextId = pretranslation.TextId,
                    Refs = { pretranslation.Refs },
                    Translation = pretranslation.Translation
                }
            );
        }
        await _outboxService.EnqueueMessageAsync(
            OutboxMessageMethod.InsertPretranslations,
            engineId,
            JsonSerializer.Serialize(requests),
            cancellationToken
        );
    }

    public async Task IncrementTrainSizeAsync(
        string engineId,
        int count = 1,
        CancellationToken cancellationToken = default
    )
    {
        await _outboxService.EnqueueMessageAsync(
            OutboxMessageMethod.IncrementTranslationEngineCorpusSize,
            engineId,
            JsonSerializer.Serialize(
                new IncrementTranslationEngineCorpusSizeRequest { EngineId = engineId, Count = count }
            ),
            cancellationToken
        );
    }
}
