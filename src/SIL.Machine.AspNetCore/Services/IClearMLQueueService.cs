namespace SIL.Machine.AspNetCore.Services;

public interface IClearMLQueueService
{
    public IDictionary<TranslationEngineType, int> QueueSizePerEngineType { get; }
}
