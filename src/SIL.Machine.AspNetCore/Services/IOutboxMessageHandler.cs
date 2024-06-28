namespace SIL.Machine.AspNetCore.Services;

public interface IOutboxMessageHandler
{
    public string OutboxId { get; }

    public Task HandleMessageAsync(
        string method,
        string? content,
        Stream? contentStream,
        CancellationToken cancellationToken = default
    );
}
