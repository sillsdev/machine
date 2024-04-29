using Serval.Translation.V1;

namespace SIL.Machine.AspNetCore.Services;

public class MessageOutboxHandlerService(
    TranslationPlatformApi.TranslationPlatformApiClient client,
    IRepository<OutboxMessage> messages,
    ISharedFileService sharedFileService,
    ILogger<MessageOutboxHandlerService> logger
) : BackgroundService
{
    private readonly TranslationPlatformApi.TranslationPlatformApiClient _client = client;
    private readonly IRepository<OutboxMessage> _messages = messages;
    private readonly ISharedFileService _sharedFileService = sharedFileService;
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

        IEnumerable<List<OutboxMessage>> messageGroups = messages.GroupBy(
            m => m.GroupId,
            m => m,
            (key, element) => element.OrderBy(m => m.Id).ToList()
        );

        foreach (List<OutboxMessage> messageGroup in messageGroups)
        {
            bool abortMessageGroup = false;
            foreach (OutboxMessage message in messageGroup)
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

    async Task ProcessGroupMessagesAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        string requestContent;
        bool deleteMessageFromDisk = false;
        if (message.RequestContent is null)
        {
            await using var requestContentStream = await _sharedFileService.OpenReadAsync(
                $"outbox/{message.Id}.json",
                cancellationToken
            );
            requestContent = new StreamReader(requestContentStream).ReadToEnd();
            deleteMessageFromDisk = true;
        }
        else
        {
            requestContent = message.RequestContent;
        }
        switch (message.Method)
        {
            case OutboxMessageMethod.BuildStarted:
                await _client.BuildStartedAsync(
                    JsonSerializer.Deserialize<BuildStartedRequest>(requestContent),
                    cancellationToken: cancellationToken
                );
                break;
            case OutboxMessageMethod.BuildCompleted:
                await _client.BuildCompletedAsync(
                    JsonSerializer.Deserialize<BuildCompletedRequest>(requestContent),
                    cancellationToken: cancellationToken
                );
                break;
            case OutboxMessageMethod.BuildCanceled:
                await _client.BuildCanceledAsync(
                    JsonSerializer.Deserialize<BuildCanceledRequest>(requestContent),
                    cancellationToken: cancellationToken
                );
                break;
            case OutboxMessageMethod.BuildFaulted:
                await _client.BuildFaultedAsync(
                    JsonSerializer.Deserialize<BuildFaultedRequest>(requestContent),
                    cancellationToken: cancellationToken
                );
                break;
            case OutboxMessageMethod.BuildRestarting:
                await _client.BuildRestartingAsync(
                    JsonSerializer.Deserialize<BuildRestartingRequest>(requestContent),
                    cancellationToken: cancellationToken
                );
                break;
            case OutboxMessageMethod.InsertPretranslations:

                {
                    using var call = _client.InsertPretranslations(cancellationToken: cancellationToken);
                    var requests = JsonSerializer.Deserialize<List<InsertPretranslationRequest>>(requestContent);
                    foreach (
                        var request in requests?.Where(r => r != null)
                            ?? Enumerable.Empty<InsertPretranslationRequest>()
                    )
                    {
                        await call.RequestStream.WriteAsync(request, cancellationToken: cancellationToken);
                    }
                    await call.RequestStream.CompleteAsync();
                }
                break;
            case OutboxMessageMethod.IncrementTranslationEngineCorpusSize:
                await _client.IncrementTranslationEngineCorpusSizeAsync(
                    JsonSerializer.Deserialize<IncrementTranslationEngineCorpusSizeRequest>(requestContent),
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
        if (deleteMessageFromDisk)
        {
            await _sharedFileService.DeleteAsync($"outbox/{message.Id}.json", cancellationToken);
        }
    }

    async Task<bool> CheckIfFinalMessageAttempt(OutboxMessage message, Exception e)
    {
        // if the message is older than 4 days, permanently fail it
        if (message.Created < DateTimeOffset.UtcNow.AddDays(-4))
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
