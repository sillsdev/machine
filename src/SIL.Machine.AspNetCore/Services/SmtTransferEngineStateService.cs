namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineStateService(
    ISmtModelFactory smtModelFactory,
    ITransferEngineFactory transferEngineFactory,
    ITruecaserFactory truecaserFactory,
    IOptionsMonitor<SmtTransferEngineOptions> options
) : AsyncDisposableBase
{
    private readonly ISmtModelFactory _smtModelFactory = smtModelFactory;
    private readonly ITransferEngineFactory _transferEngineFactory = transferEngineFactory;
    private readonly ITruecaserFactory _truecaserFactory = truecaserFactory;
    private readonly IOptionsMonitor<SmtTransferEngineOptions> _options = options;

    private readonly ConcurrentDictionary<string, SmtTransferEngineState> _engineStates =
        new ConcurrentDictionary<string, SmtTransferEngineState>();

    public SmtTransferEngineState Get(string engineId)
    {
        return _engineStates.GetOrAdd(engineId, CreateState);
    }

    public bool TryRemove(string engineId, [MaybeNullWhen(false)] out SmtTransferEngineState state)
    {
        return _engineStates.TryRemove(engineId, out state);
    }

    public async Task CommitAsync(
        IDistributedReaderWriterLockFactory lockFactory,
        IRepository<TranslationEngine> engines,
        TimeSpan inactiveTimeout,
        CancellationToken cancellationToken = default
    )
    {
        foreach (SmtTransferEngineState state in _engineStates.Values)
        {
            IDistributedReaderWriterLock @lock = await lockFactory.CreateAsync(state.EngineId, cancellationToken);
            await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
            {
                TranslationEngine? engine = await engines.GetAsync(
                    e => e.EngineId == state.EngineId,
                    cancellationToken
                );
                if (
                    engine is not null
                    && (engine.CurrentBuild is null || engine.CurrentBuild.JobState is BuildJobState.Pending)
                )
                {
                    await state.CommitAsync(engine.BuildRevision, inactiveTimeout, cancellationToken);
                }
            }
        }
    }

    private SmtTransferEngineState CreateState(string engineId)
    {
        return new SmtTransferEngineState(
            _smtModelFactory,
            _transferEngineFactory,
            _truecaserFactory,
            _options,
            engineId
        );
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        foreach (SmtTransferEngineState state in _engineStates.Values)
            await state.DisposeAsync();
        _engineStates.Clear();
    }
}
