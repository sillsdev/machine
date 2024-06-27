namespace SIL.Machine.AspNetCore.Services;

public class MessageOutboxDeliveryService(
    IRepository<OutboxMessage> messages,
    IEnumerable<IOutboxMessageHandler> outboxMessageHandlers,
    IOptionsMonitor<MessageOutboxOptions> options,
    ILogger<MessageOutboxDeliveryService> logger
) : BackgroundService
{
    private readonly IRepository<OutboxMessage> _messages = messages;
    private readonly Dictionary<string, IOutboxMessageHandler> _outboxMessageHandlers =
        outboxMessageHandlers.ToDictionary(o => o.Name);

    private readonly ILogger<MessageOutboxDeliveryService> _logger = logger;
    protected TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    protected TimeSpan MessageExpiration { get; set; } =
        TimeSpan.FromHours(options.CurrentValue.MessageExpirationInHours);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using ISubscription<OutboxMessage> subscription = await _messages.SubscribeAsync(e => true);
        while (true)
        {
            await subscription.WaitForChangeAsync(timeout: Timeout, cancellationToken: stoppingToken);
            if (stoppingToken.IsCancellationRequested)
                break;
            await ProcessMessagesAsync();
        }
    }

    protected async Task ProcessMessagesAsync(CancellationToken cancellationToken = default)
    {
        bool anyMessages = await _messages.ExistsAsync(m => true);
        if (!anyMessages)
            return;

        IReadOnlyList<OutboxMessage> messages = await _messages.GetAllAsync();

        IEnumerable<List<OutboxMessage>> messageGroups = messages.GroupBy(
            m => new { m.GroupId, m.OutboxName },
            m => m,
            (key, element) => element.OrderBy(m => m.Index).ToList()
        );

        foreach (List<OutboxMessage> messageGroup in messageGroups)
        {
            bool abortMessageGroup = false;
            var outboxMessageHandler = _outboxMessageHandlers[messageGroup.First().OutboxName];
            foreach (OutboxMessage message in messageGroup)
            {
                try
                {
                    await ProcessGroupMessagesAsync(message, outboxMessageHandler, cancellationToken);
                }
                catch (RpcException e)
                {
                    switch (e.StatusCode)
                    {
                        case StatusCode.Unavailable:
                        case StatusCode.Unauthenticated:
                        case StatusCode.PermissionDenied:
                        case StatusCode.Cancelled:
                            _logger.LogWarning(e, "Platform Message sending failure: {statusCode}", e.StatusCode);
                            return;
                        case StatusCode.Aborted:
                        case StatusCode.DeadlineExceeded:
                        case StatusCode.Internal:
                        case StatusCode.ResourceExhausted:
                        case StatusCode.Unknown:
                            abortMessageGroup = !await CheckIfFinalMessageAttempt(message, e);
                            break;
                        case StatusCode.InvalidArgument:
                        default:
                            // log error
                            await PermanentlyFailedMessage(message, e);
                            break;
                    }
                }
                catch (Exception e)
                {
                    await PermanentlyFailedMessage(message, e);
                    break;
                }
                if (abortMessageGroup)
                    break;
            }
        }
    }

    async Task ProcessGroupMessagesAsync(
        OutboxMessage message,
        IOutboxMessageHandler outboxMessageHandler,
        CancellationToken cancellationToken = default
    )
    {
        await outboxMessageHandler.SendMessageAsync(message, cancellationToken);
        await _messages.DeleteAsync(message.Id);
        await outboxMessageHandler.CleanupMessageAsync(message, cancellationToken);
    }

    async Task<bool> CheckIfFinalMessageAttempt(OutboxMessage message, Exception e)
    {
        if (message.Created < DateTimeOffset.UtcNow.Subtract(MessageExpiration))
        {
            await PermanentlyFailedMessage(message, e);
            return true;
        }
        else
        {
            await LogFailedAttempt(message, e);
            return false;
        }
    }

    async Task PermanentlyFailedMessage(OutboxMessage message, Exception e)
    {
        // log error
        _logger.LogError(
            e,
            "Permanently failed to process message {message.Id}: {message.Method} with content {message.RequestContent} and error message: {e.Message}",
            message.Id,
            message.Method,
            message.RequestContent,
            e.Message
        );
        await _messages.DeleteAsync(message.Id);
    }

    async Task LogFailedAttempt(OutboxMessage message, Exception e)
    {
        // log error
        await _messages.UpdateAsync(m => m.Id == message.Id, b => b.Inc(m => m.Attempts, 1));
        _logger.LogError(
            e,
            "Attempt {message.Attempts}.  Failed to process message {message.Id}: {message.Method} with content {message.RequestContent} and error message: {e.Message}",
            message.Attempts + 1,
            message.Id,
            message.Method,
            message.RequestContent,
            e.Message
        );
    }
}
