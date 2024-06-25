namespace SIL.Machine.AspNetCore.Services;

public class MessageOutboxService(
    IRepository<Sequence> messageIndexes,
    IRepository<OutboxMessage> messages,
    ISharedFileService sharedFileService
) : IMessageOutboxService
{
    private readonly IRepository<Sequence> _messageIndex = messageIndexes;
    private readonly IRepository<OutboxMessage> _messages = messages;
    private readonly ISharedFileService _sharedFileService = sharedFileService;
    protected int MaxDocumentSize { get; set; } = 1_000_000;

    public async Task<string> EnqueueMessageAsync(
        OutboxMessageMethod method,
        string groupId,
        string requestContent,
        bool alwaysSaveContentToDisk = false,
        CancellationToken cancellationToken = default
    )
    {
        // get next index
        Sequence outboxIndex = (
            await _messageIndex.UpdateAsync(
                i => i.Context == "MessageOutbox",
                i => i.Inc(b => b.CurrentIndex, 1),
                upsert: true,
                cancellationToken: cancellationToken
            )
        )!;
        string id = Sequence.IndexToObjectIdString(outboxIndex.CurrentIndex);
        OutboxMessage outboxMessage = new OutboxMessage
        {
            Id = id,
            Method = method,
            GroupId = groupId,
            RequestContent = requestContent
        };
        if (requestContent.Length > MaxDocumentSize || alwaysSaveContentToDisk)
        {
            // The file is too large - save it to disk and send a reference.
            // MongoDB has a 16MB document size limit - let's keep below that.
            await using StreamWriter sourceTrainWriter =
                new(await _sharedFileService.OpenWriteAsync($"outbox/{id}.json", cancellationToken));
            sourceTrainWriter.Write(requestContent);
            outboxMessage.RequestContent = null;
        }
        await _messages.InsertAsync(outboxMessage, cancellationToken: cancellationToken);
        return id;
    }
}
