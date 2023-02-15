using Google.Protobuf.WellKnownTypes;
using Serval.Core;
using Serval.Platform.BuildStatus.V1;

namespace Serval.AspNetCore.Services;

public class ServalBuildStatusService : BuildStatusService.BuildStatusServiceBase
{
    private static readonly Empty Empty = new();

    private readonly IRepository<Build> _builds;
    private readonly IRepository<TranslationEngine> _engines;
    private readonly IWebhookService _webhookService;

    public ServalBuildStatusService(
        IRepository<Build> builds,
        IRepository<TranslationEngine> engines,
        IWebhookService webhookService
    )
    {
        _builds = builds;
        _engines = engines;
        _webhookService = webhookService;
    }

    public override async Task<Empty> BuildStarted(BuildStartedRequest request, ServerCallContext context)
    {
        Build? build = await _builds.UpdateAsync(
            b => b.Id == request.BuildId,
            u => u.Set(b => b.State, BuildState.Active),
            cancellationToken: context.CancellationToken
        );
        if (build is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The build does not exist."));

        TranslationEngine? engine = await _engines.UpdateAsync(
            build.ParentRef,
            u => u.Set(e => e.IsBuilding, true),
            cancellationToken: context.CancellationToken
        );
        if (engine is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The engine does not exist."));

        await _webhookService.SendEventAsync(WebhookEvent.BuildStarted, engine.Owner, build);

        return Empty;
    }

    public override async Task<Empty> BuildCompleted(BuildCompletedRequest request, ServerCallContext context)
    {
        Build? build = await _builds.UpdateAsync(
            request.BuildId,
            u =>
                u.Set(b => b.State, BuildState.Completed)
                    .Set(b => b.Message, "Completed")
                    .Set(b => b.DateFinished, DateTime.UtcNow),
            cancellationToken: context.CancellationToken
        );
        if (build is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The build does not exist."));

        TranslationEngine? engine = await _engines.UpdateAsync(
            build.ParentRef,
            u =>
                u.Set(e => e.Confidence, request.Confidence)
                    .Set(e => e.CorpusSize, request.CorpusSize)
                    .Set(e => e.IsBuilding, false)
                    .Inc(e => e.ModelRevision),
            cancellationToken: context.CancellationToken
        );
        if (engine is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The engine does not exist."));

        await _webhookService.SendEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);

        return Empty;
    }

    public override async Task<Empty> BuildCanceled(BuildCanceledRequest request, ServerCallContext context)
    {
        Build? build = await _builds.UpdateAsync(
            b => b.Id == request.BuildId && b.DateFinished == null,
            u => u.Set(b => b.DateFinished, DateTime.UtcNow),
            cancellationToken: context.CancellationToken
        );
        if (build is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The build does not exist."));

        TranslationEngine? engine = await _engines.UpdateAsync(
            build.ParentRef,
            u => u.Set(e => e.IsBuilding, false),
            cancellationToken: context.CancellationToken
        );
        if (engine is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The engine does not exist."));

        await _webhookService.SendEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);

        return Empty;
    }

    public override async Task<Empty> BuildFaulted(BuildFaultedRequest request, ServerCallContext context)
    {
        Build? build = await _builds.UpdateAsync(
            request.BuildId,
            u =>
                u.Set(b => b.State, BuildState.Faulted)
                    .Set(b => b.Message, request.Message)
                    .Set(b => b.DateFinished, DateTime.UtcNow),
            cancellationToken: context.CancellationToken
        );
        if (build is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The build does not exist."));

        TranslationEngine? engine = await _engines.UpdateAsync(
            build.ParentRef,
            u => u.Set(e => e.IsBuilding, false),
            cancellationToken: context.CancellationToken
        );
        if (engine is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The engine does not exist."));

        await _webhookService.SendEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);

        return Empty;
    }

    public override async Task<Empty> BuildRestarting(BuildRestartingRequest request, ServerCallContext context)
    {
        Build? build = await _builds.UpdateAsync(
            request.BuildId,
            u =>
                u.Set(b => b.Message, "Restarting")
                    .Set(b => b.Step, 0)
                    .Set(b => b.PercentCompleted, 0)
                    .Set(b => b.State, BuildState.Pending),
            cancellationToken: context.CancellationToken
        );
        if (build is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The build does not exist."));

        return Empty;
    }

    public override async Task<Empty> UpdateBuildStatus(UpdateBuildStatusRequest request, ServerCallContext context)
    {
        await _builds.UpdateAsync(
            b => b.Id == request.BuildId && b.State == BuildState.Active,
            u =>
            {
                u.Set(b => b.Step, request.Step);
                if (request.HasPercentCompleted)
                {
                    u.Set(
                        b => b.PercentCompleted,
                        Math.Round(request.PercentCompleted, 4, MidpointRounding.AwayFromZero)
                    );
                }
                if (request.HasMessage)
                    u.Set(b => b.Message, request.Message);
            },
            cancellationToken: context.CancellationToken
        );

        return Empty;
    }
}
