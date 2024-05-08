namespace SIL.Machine.AspNetCore.Services;

public interface IMessageOutboxService
{
    public Task<string> EnqueueMessageAsync(
        OutboxMessageMethod method,
        string groupId,
        string requestContent,
        CancellationToken cancellationToken
    );
}
