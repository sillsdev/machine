namespace SIL.Machine.AspNetCore.Services;

public interface ICleanupOldModelsJob
{
    public Task RunAsync();
}
