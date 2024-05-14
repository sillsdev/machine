namespace SIL.Machine.AspNetCore.Services;

public interface IClearMLQueueService
{
    public int GetQueueSize(TranslationEngineType engineType);
}
