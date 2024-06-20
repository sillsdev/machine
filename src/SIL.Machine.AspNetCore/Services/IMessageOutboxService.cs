namespace SIL.Machine.AspNetCore.Services;

public interface IMessageOutboxService
{
    public Task<string> EnqueueMessageAsync<T>(
        T method,
        string groupId,
        string? requestContent = null,
        string? requestContentPath = null,
        CancellationToken cancellationToken = default
    );
}
