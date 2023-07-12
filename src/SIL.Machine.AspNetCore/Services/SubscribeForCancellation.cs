namespace SIL.Machine.AspNetCore.Services;

public class SubscribeForCancellation
{
    private readonly IRepository<TranslationEngine> _engines;

    public SubscribeForCancellation(IRepository<TranslationEngine> engines)
    {
        _engines = engines;
    }

    public CancellationToken GetCombinedCancellationToken(
        string engineId,
        string buildId,
        CancellationToken externalCancellationToken
    )
    {
        CancellationTokenSource cts = new();
        SubscribeForCancellationAsync(cts, engineId, buildId);
        CancellationTokenSource combinedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
            externalCancellationToken,
            cts.Token
        );
        return combinedCancellationSource.Token;
    }

    private async void SubscribeForCancellationAsync(CancellationTokenSource cts, string engineId, string buildId)
    {
        var cancellationToken = cts.Token;
        ISubscription<TranslationEngine> sub = await _engines.SubscribeAsync(
            e => e.EngineId == engineId && e.BuildId == buildId
        );
        if (sub.Change.Entity is null)
            return;
        while (true)
        {
            await sub.WaitForChangeAsync(TimeSpan.FromSeconds(10), cancellationToken);
            TranslationEngine? engine = sub.Change.Entity;
            if (engine is null || engine.IsCanceled)
            {
                cts.Cancel();
                return;
            }
            if (cancellationToken.IsCancellationRequested)
                return;
            Thread.Sleep(500);
        }
    }
}
