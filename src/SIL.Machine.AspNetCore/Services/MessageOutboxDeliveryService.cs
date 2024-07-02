namespace SIL.Machine.AspNetCore.Services;

public class MessageOutboxDeliveryService(
    IServiceProvider services,
    IEnumerable<IOutboxMessageHandler> outboxMessageHandlers,
    IFileSystem fileSystem,
    IOptionsMonitor<MessageOutboxOptions> options,
    ILogger<MessageOutboxDeliveryService> logger
) : BackgroundService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly IServiceProvider _services = services;
    private readonly Dictionary<string, IOutboxMessageHandler> _outboxMessageHandlers =
        outboxMessageHandlers.ToDictionary(o => o.OutboxId);
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly IOptionsMonitor<MessageOutboxOptions> _options = options;
    private readonly ILogger<MessageOutboxDeliveryService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Initialize();
        using IServiceScope scope = _services.CreateScope();
        var messages = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
        using ISubscription<OutboxMessage> subscription = await messages.SubscribeAsync(e => true, stoppingToken);
        while (true)
        {
            await subscription.WaitForChangeAsync(timeout: Timeout, cancellationToken: stoppingToken);
            if (stoppingToken.IsCancellationRequested)
                break;
            await ProcessMessagesAsync(messages, stoppingToken);
        }
    }

    private void Initialize()
    {
        _fileSystem.CreateDirectory(_options.CurrentValue.OutboxDir);
    }

    internal async Task ProcessMessagesAsync(
        IRepository<OutboxMessage> messages,
        CancellationToken cancellationToken = default
    )
    {
        bool anyMessages = await messages.ExistsAsync(m => true, cancellationToken);
        if (!anyMessages)
            return;

        IReadOnlyList<OutboxMessage> curMessages = await messages.GetAllAsync(cancellationToken);

        IEnumerable<IGrouping<(string GroupId, string OutboxRef), OutboxMessage>> messageGroups = curMessages
            .OrderBy(m => m.Index)
            .GroupBy(m => (m.OutboxRef, m.GroupId));

        foreach (IGrouping<(string OutboxId, string GroupId), OutboxMessage> messageGroup in messageGroups)
        {
            bool abortMessageGroup = false;
            IOutboxMessageHandler outboxMessageHandler = _outboxMessageHandlers[messageGroup.Key.OutboxId];
            foreach (OutboxMessage message in messageGroup)
            {
                try
                {
                    await ProcessGroupMessagesAsync(messages, message, outboxMessageHandler, cancellationToken);
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
                            abortMessageGroup = !await CheckIfFinalMessageAttempt(messages, message, e);
                            break;
                        case StatusCode.InvalidArgument:
                        default:
                            // log error
                            await PermanentlyFailedMessage(messages, message, e);
                            break;
                    }
                }
                catch (Exception e)
                {
                    await PermanentlyFailedMessage(messages, message, e);
                    break;
                }
                if (abortMessageGroup)
                    break;
            }
        }
    }

    private async Task ProcessGroupMessagesAsync(
        IRepository<OutboxMessage> messages,
        OutboxMessage message,
        IOutboxMessageHandler outboxMessageHandler,
        CancellationToken cancellationToken = default
    )
    {
        Stream? contentStream = null;
        string filePath = Path.Combine(_options.CurrentValue.OutboxDir, message.Id);
        if (message.HasContentStream)
            contentStream = _fileSystem.OpenRead(filePath);
        try
        {
            await outboxMessageHandler.HandleMessageAsync(
                message.Method,
                message.Content,
                contentStream,
                cancellationToken
            );
            await messages.DeleteAsync(message.Id);
        }
        finally
        {
            contentStream?.Dispose();
        }
        _fileSystem.DeleteFile(filePath);
    }

    private async Task<bool> CheckIfFinalMessageAttempt(
        IRepository<OutboxMessage> messages,
        OutboxMessage message,
        Exception e
    )
    {
        if (message.Created < DateTimeOffset.UtcNow.Subtract(_options.CurrentValue.MessageExpirationTimeout))
        {
            await PermanentlyFailedMessage(messages, message, e);
            return true;
        }
        else
        {
            await LogFailedAttempt(messages, message, e);
            return false;
        }
    }

    private async Task PermanentlyFailedMessage(IRepository<OutboxMessage> messages, OutboxMessage message, Exception e)
    {
        // log error
        _logger.LogError(
            e,
            "Permanently failed to process message {Id}: {Method} with content {Content} and error message: {ErrorMessage}",
            message.Id,
            message.Method,
            message.Content,
            e.Message
        );
        await messages.DeleteAsync(message.Id);
    }

    private async Task LogFailedAttempt(IRepository<OutboxMessage> messages, OutboxMessage message, Exception e)
    {
        // log error
        await messages.UpdateAsync(m => m.Id == message.Id, b => b.Inc(m => m.Attempts, 1));
        _logger.LogError(
            e,
            "Attempt {Attempts}.  Failed to process message {Id}: {Method} with content {Content} and error message: {ErrorMessage}",
            message.Attempts + 1,
            message.Id,
            message.Method,
            message.Content,
            e.Message
        );
    }
}
