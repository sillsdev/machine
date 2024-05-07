namespace SIL.Machine.AspNetCore.Services;

public class MessageOutboxService(IRepository<OutboxMessage> messages) : IMessageOutboxService
{
    private readonly IRepository<OutboxMessage> _messages = messages;

    public async Task EnqueueMessageAsync(
        OutboxMessageMethod method,
        string groupId,
        string requestContent,
        CancellationToken cancellationToken
    )
    {
        await _messages.InsertAsync(
            new OutboxMessage
            {
                Method = method,
                GroupId = groupId,
                RequestContent = requestContent
            },
            cancellationToken: cancellationToken
        );
    }
}
