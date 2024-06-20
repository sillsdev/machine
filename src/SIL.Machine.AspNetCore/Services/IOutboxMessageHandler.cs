namespace SIL.Machine.AspNetCore.Services;

public interface IOutboxMessageHandler
{
    public string Name { get; }

    public Task SendMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    public Task CleanupMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
