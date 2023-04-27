namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineStateService : AsyncDisposableBase
{
    private readonly ISmtModelFactory _smtModelFactory;
    private readonly ITransferEngineFactory _transferEngineFactory;
    private readonly ITruecaserFactory _truecaserFactory;

    private readonly ConcurrentDictionary<string, SmtTransferEngineState> _engineStates;

    public SmtTransferEngineStateService(
        ISmtModelFactory smtModelFactory,
        ITransferEngineFactory transferEngineFactory,
        ITruecaserFactory truecaserFactory
    )
    {
        _smtModelFactory = smtModelFactory;
        _transferEngineFactory = transferEngineFactory;
        _truecaserFactory = truecaserFactory;
        _engineStates = new ConcurrentDictionary<string, SmtTransferEngineState>();
    }

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
        TimeSpan inactiveTimeout
    )
    {
        foreach (SmtTransferEngineState state in _engineStates.Values)
        {
            IDistributedReaderWriterLock @lock = lockFactory.Create(state.EngineId);
            await using (await @lock.WriterLockAsync())
            {
                TranslationEngine? engine = await engines.GetAsync(e => e.EngineId == state.EngineId);
                if (engine is not null && engine.BuildState is not BuildState.Active)
                    await state.CommitAsync(engine.BuildRevision, inactiveTimeout);
            }
        }
    }

    private SmtTransferEngineState CreateState(string engineId)
    {
        return new SmtTransferEngineState(_smtModelFactory, _transferEngineFactory, _truecaserFactory, engineId);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        foreach (SmtTransferEngineState state in _engineStates.Values)
            await state.DisposeAsync();
        _engineStates.Clear();
    }
}
