namespace SIL.Machine.AspNetCore.Services;

public interface IMessageOutboxService
{
    public Task<string> EnqueueMessageAsync(
        string outboxId,
        string method,
        string groupId,
        string? content = null,
        Stream? contentStream = null,
        CancellationToken cancellationToken = default
    );
}
