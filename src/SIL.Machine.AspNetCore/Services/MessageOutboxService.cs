using MongoDB.Bson;

namespace SIL.Machine.AspNetCore.Services;

public class MessageOutboxService(
    IRepository<SortableIndex> messageIndexes,
    IRepository<OutboxMessage> messages,
    ISharedFileService sharedFileService
) : IMessageOutboxService
{
    private readonly IRepository<SortableIndex> _messageIndex = messageIndexes;
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
        string sortableIndex = await SortableIndex.GetSortableIndexAsync(
            _messageIndex,
            "MessageOutbox",
            cancellationToken
        );
        OutboxMessage outboxMessage = new OutboxMessage
        {
            Id = ObjectId.GenerateNewId().ToString(),
            SortableIndex = sortableIndex,
            Method = method,
            GroupId = groupId,
            RequestContent = requestContent
        };
        if (requestContent.Length > MaxDocumentSize || alwaysSaveContentToDisk)
        {
            // The file is too large - save it to disk and send a reference.
            // MongoDB has a 16MB document size limit - let's keep below that.
            await using StreamWriter sourceTrainWriter =
                new(await _sharedFileService.OpenWriteAsync($"outbox/{outboxMessage.Id}", cancellationToken));
            sourceTrainWriter.Write(requestContent);
            outboxMessage.RequestContent = null;
        }
        await _messages.InsertAsync(outboxMessage, cancellationToken: cancellationToken);
        return outboxMessage.Id;
    }
}
