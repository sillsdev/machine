using Serval.Translation.V1;

namespace SIL.Machine.AspNetCore.Services;

public class MessageOutboxHandlerService(
    TranslationPlatformApi.TranslationPlatformApiClient client,
    IRepository<OutboxMessage> messages,
    ILogger<MessageOutboxHandlerService> logger
) : BackgroundService
{
    private readonly TranslationPlatformApi.TranslationPlatformApiClient _client = client;
    private readonly IRepository<OutboxMessage> _messages = messages;
    private readonly ILogger<MessageOutboxHandlerService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using ISubscription<OutboxMessage> subscription = await _messages.SubscribeAsync(e => true);
        while (true)
        {
            await subscription.WaitForChangeAsync(timeout: TimeSpan.FromSeconds(10), cancellationToken: stoppingToken);
            if (stoppingToken.IsCancellationRequested)
                break;
            await ProcessMessagesAsync();
        }
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken = default)
    {
        bool anyMessages = await _messages.ExistsAsync(m => true);
        if (!anyMessages)
            return;

        IReadOnlyList<OutboxMessage> messages = await _messages.GetAllAsync();

        IEnumerable<List<OutboxMessage>> groupedMessages = messages.GroupBy(
            m => m.GroupId,
            m => m,
            (key, element) => element.OrderBy(m => m.Created).ToList()
        );

        foreach (List<OutboxMessage> group in groupedMessages)
        {
            bool abortMessageGroup = false;
            foreach (OutboxMessage message in messages)
            {
                try
                {
                    await ProcessGroupMessagesAsync(message, cancellationToken);
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
                            abortMessageGroup = await CheckIfFinalMessageAttempt(message, e);
                            break;
                        default:
                            // log error
                            await PermanentlyFailedMessage(message, e);
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unspecified platform Message sending failure.");
                    return;
                }
                if (abortMessageGroup)
                    break;
            }
        }
    }

    async Task ProcessGroupMessagesAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        switch (message.Method)
        {
            case OutboxMessageMethod.BuildStarted:
                await _client.BuildStartedAsync(
                    JsonSerializer.Deserialize<BuildStartedRequest>(message.RequestContent),
                    cancellationToken: cancellationToken
                );
                break;
            case OutboxMessageMethod.BuildCompleted:
                await _client.BuildCompletedAsync(
                    JsonSerializer.Deserialize<BuildCompletedRequest>(message.RequestContent),
                    cancellationToken: cancellationToken
                );
                break;
            case OutboxMessageMethod.BuildCanceled:
                await _client.BuildCanceledAsync(
                    JsonSerializer.Deserialize<BuildCanceledRequest>(message.RequestContent),
                    cancellationToken: cancellationToken
                );
                break;
            case OutboxMessageMethod.BuildFaulted:
                await _client.BuildFaultedAsync(
                    JsonSerializer.Deserialize<BuildFaultedRequest>(message.RequestContent),
                    cancellationToken: cancellationToken
                );
                break;
            case OutboxMessageMethod.BuildRestarting:
                await _client.BuildRestartingAsync(
                    JsonSerializer.Deserialize<BuildRestartingRequest>(message.RequestContent),
                    cancellationToken: cancellationToken
                );
                break;
            case OutboxMessageMethod.InsertPretranslations:

                {
                    using var call = _client.InsertPretranslations(cancellationToken: cancellationToken);
                    var requests = JsonSerializer.Deserialize<List<InsertPretranslationRequest>>(
                        message.RequestContent
                    );
                    foreach (
                        var request in requests?.Where(r => r != null)
                            ?? System.Linq.Enumerable.Empty<InsertPretranslationRequest>()
                    )
                    {
                        await call.RequestStream.WriteAsync(request, cancellationToken: cancellationToken);
                    }
                    await call.RequestStream.CompleteAsync();
                }
                break;
            case OutboxMessageMethod.IncrementTranslationEngineCorpusSize:
                await _client.IncrementTranslationEngineCorpusSizeAsync(
                    JsonSerializer.Deserialize<IncrementTranslationEngineCorpusSizeRequest>(message.RequestContent),
                    cancellationToken: cancellationToken
                );
                break;
            default:
                await _messages.DeleteAsync(message.Id);
                _logger.LogWarning(
                    "Unknown method: {message.Method}.  Deleting the message from the list.",
                    message.Method.ToString()
                );
                break;
        }
        await _messages.DeleteAsync(message.Id);
    }

    async Task<bool> CheckIfFinalMessageAttempt(OutboxMessage message, Exception e)
    {
        if (message.Attempts > 3) // will fail the 5th time
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
            "Permanently failed to process message {message.Id}: {message.Method} with content {message.RequestContent}",
            message.Id,
            message.Method,
            message.RequestContent
        );
        await _messages.DeleteAsync(message.Id);
    }

    async Task LogFailedAttempt(OutboxMessage message, Exception e)
    {
        // log error
        await _messages.UpdateAsync(m => m.Id == message.Id, b => b.Inc(m => m.Attempts, 1));
        _logger.LogError(
            e,
            "Attempt {message.Attempts}.  Failed to process message {message.Id}: {message.Method} with content {message.RequestContent}",
            message.Attempts + 1,
            message.Id,
            message.Method,
            message.RequestContent
        );
    }
}
