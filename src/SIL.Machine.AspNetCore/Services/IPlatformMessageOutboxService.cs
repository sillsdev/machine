namespace SIL.Machine.AspNetCore.Services;

public interface IPlatformMessageOutboxService : IHostedService
{
    public Task EnqueueMessageAsync(string method, string requestContent, CancellationToken cancellationToken);
}
