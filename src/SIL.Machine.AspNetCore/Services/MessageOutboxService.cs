using MongoDB.Bson;

namespace SIL.Machine.AspNetCore.Services;

public class MessageOutboxService(
    IRepository<Outbox> messageIndexes,
    IRepository<OutboxMessage> messages,
    ISharedFileService sharedFileService
) : IMessageOutboxService
{
    private readonly IRepository<Outbox> _messageIndex = messageIndexes;
    private readonly IRepository<OutboxMessage> _messages = messages;
    private readonly ISharedFileService _sharedFileService = sharedFileService;
    protected int MaxDocumentSize { get; set; } = 1_000_000;

    public async Task<string> EnqueueMessageAsync<T>(
        T method,
        string groupId,
        string? requestContent = null,
        string? requestContentPath = null,
        CancellationToken cancellationToken = default
    )
    {
        if (requestContent == null && requestContentPath == null)
        {
            throw new ArgumentException("Either requestContent or contentPath must be specified.");
        }
        if (requestContent is not null && requestContent.Length > MaxDocumentSize)
        {
            throw new ArgumentException(
                $"The content is too large for request {method} with group ID {groupId}. "
                    + $"It is {requestContent.Length} bytes, but the maximum is {MaxDocumentSize} bytes."
            );
        }
        Outbox outbox = await Outbox.GetOutboxNextIndexAsync(_messageIndex, typeof(T).ToString(), cancellationToken);
        OutboxMessage outboxMessage = new OutboxMessage
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Index = outbox.CurrentIndex,
            OutboxName = typeof(T).ToString(),
            Method = method?.ToString() ?? throw new ArgumentNullException(nameof(method)),
            GroupId = groupId,
            RequestContent = requestContent
        };
        if (requestContentPath != null)
        {
            await _sharedFileService.MoveAsync(requestContentPath, $"outbox/{outboxMessage.Id}", cancellationToken);
        }
        await _messages.InsertAsync(outboxMessage, cancellationToken: cancellationToken);
        return outboxMessage.Id;
    }
}
