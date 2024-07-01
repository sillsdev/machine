namespace SIL.Machine.AspNetCore.Services;

public class MessageOutboxService(
    IRepository<Outbox> outboxes,
    IRepository<OutboxMessage> messages,
    IIdGenerator idGenerator,
    IFileSystem fileSystem,
    IOptionsMonitor<MessageOutboxOptions> options
) : IMessageOutboxService
{
    private readonly IRepository<Outbox> _outboxes = outboxes;
    private readonly IRepository<OutboxMessage> _messages = messages;
    private readonly IIdGenerator _idGenerator = idGenerator;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly IOptionsMonitor<MessageOutboxOptions> _options = options;
    internal int MaxDocumentSize { get; set; } = 1_000_000;

    public async Task<string> EnqueueMessageAsync(
        string outboxId,
        string method,
        string groupId,
        string? content = null,
        Stream? contentStream = null,
        CancellationToken cancellationToken = default
    )
    {
        if (content == null && contentStream == null)
        {
            throw new ArgumentException("Either content or contentStream must be specified.");
        }
        if (content is not null && content.Length > MaxDocumentSize)
        {
            throw new ArgumentException(
                $"The content is too large for request {method} with group ID {groupId}. "
                    + $"It is {content.Length} bytes, but the maximum is {MaxDocumentSize} bytes."
            );
        }
        Outbox outbox = (
            await _outboxes.UpdateAsync(
                outboxId,
                u => u.Inc(o => o.CurrentIndex, 1),
                upsert: true,
                cancellationToken: cancellationToken
            )
        )!;
        OutboxMessage outboxMessage =
            new()
            {
                Id = _idGenerator.GenerateId(),
                Index = outbox.CurrentIndex,
                OutboxRef = outboxId,
                Method = method,
                GroupId = groupId,
                Content = content,
                HasContentStream = contentStream is not null
            };
        string filePath = Path.Combine(_options.CurrentValue.OutboxDir, outboxMessage.Id);
        try
        {
            if (contentStream is not null)
            {
                await using Stream fileStream = _fileSystem.OpenWrite(filePath);
                await contentStream.CopyToAsync(fileStream, cancellationToken);
            }
            await _messages.InsertAsync(outboxMessage, cancellationToken: cancellationToken);
            return outboxMessage.Id;
        }
        catch
        {
            if (contentStream is not null)
                _fileSystem.DeleteFile(filePath);
            throw;
        }
    }
}
