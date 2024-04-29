using MongoDB.Bson;

namespace SIL.Machine.AspNetCore.Services;

public class MessageOutboxService(IRepository<OutboxMessage> messages, ISharedFileService sharedFileService)
    : IMessageOutboxService
{
    private readonly IRepository<OutboxMessage> _messages = messages;
    private readonly ISharedFileService _sharedFileService = sharedFileService;

    public async Task EnqueueMessageAsync(
        OutboxMessageMethod method,
        string groupId,
        string requestContent,
        CancellationToken cancellationToken
    )
    {
        var id = ObjectId.GenerateNewId().ToString();
        OutboxMessage outboxMessage = new OutboxMessage
        {
            Id = id,
            Method = method,
            GroupId = groupId,
            RequestContent = requestContent
        };
        if (requestContent.Length > 1_000_000)
        {
            // The file is too large - save it to disk and send a reference.
            // MongoDB has a 16MB document size limit - let's keep below that.
            await using StreamWriter sourceTrainWriter =
                new(await _sharedFileService.OpenWriteAsync($"outbox/{id}.json", cancellationToken));
            sourceTrainWriter.Write(requestContent);
            outboxMessage.RequestContent = null;
        }
        await _messages.InsertAsync(outboxMessage, cancellationToken: cancellationToken);
    }
}
