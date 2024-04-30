using Serval.Translation.V1;

namespace SIL.Machine.AspNetCore.Services;

public class PlatformMessageOutboxService(
    TranslationPlatformApi.TranslationPlatformApiClient client,
    IServiceProvider services,
    IRepository<PlatformMessage> messages,
    ILogger<PlatformMessageOutboxService> logger
)
    : RecurrentTask(
        "Platform Message Outbox Service",
        services,
        period: TimeSpan.FromSeconds(10),
        logger: logger,
        enable: true
    ),
        IPlatformMessageOutboxService
{
    private readonly TranslationPlatformApi.TranslationPlatformApiClient _client = client;
    private readonly IRepository<PlatformMessage> _messages = messages;
    private readonly ILogger<PlatformMessageOutboxService> _logger = logger;
    private bool _messagesInOutbox = true;
    private bool _processingMessages = false;
    private readonly object _lock = new();

    public async Task EnqueueMessageAsync(string method, string requestContent, CancellationToken cancellationToken)
    {
        await _messages.InsertAsync(
            new PlatformMessage { Method = method, RequestContent = requestContent },
            cancellationToken: cancellationToken
        );
        _messagesInOutbox = true;
        await ProcessMessagesAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        await ProcessMessagesAsync(cancellationToken);
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_processingMessages == true)
                return;
            _processingMessages = true;
        }

        try
        {
            await ProcessMessagesInternalAsync(cancellationToken);
        }
        finally
        {
            _processingMessages = false;
        }
    }

    private async Task ProcessMessagesInternalAsync(CancellationToken cancellationToken = default)
    {
        if (!_messagesInOutbox)
            return;

        bool allMessagesSuccessfullySent = true;
        IReadOnlyList<PlatformMessage> messages = await _messages.GetAllAsync();

        async Task FailedMessageAttempt(PlatformMessage message, Exception e)
        {
            if (message.Attempts > 5)
            {
                await PermanentlyFailedMessage(message, e);
            }
            else
            {
                await LogFailedAttempt(message, e);
                allMessagesSuccessfullySent = false;
            }
        }

        foreach (PlatformMessage message in messages)
        {
            try
            {
                switch (message.Method)
                {
                    case "BuildStartedAsync":
                        await _client.BuildStartedAsync(
                            JsonSerializer.Deserialize<BuildStartedRequest>(message.RequestContent),
                            cancellationToken: cancellationToken
                        );
                        break;
                    case "BuildCompletedAsync":
                        await _client.BuildCompletedAsync(
                            JsonSerializer.Deserialize<BuildCompletedRequest>(message.RequestContent),
                            cancellationToken: cancellationToken
                        );
                        break;
                    case "BuildCanceledAsync":
                        await _client.BuildCanceledAsync(
                            JsonSerializer.Deserialize<BuildCanceledRequest>(message.RequestContent),
                            cancellationToken: cancellationToken
                        );
                        break;
                    case "BuildFaultedAsync":
                        await _client.BuildFaultedAsync(
                            JsonSerializer.Deserialize<BuildFaultedRequest>(message.RequestContent),
                            cancellationToken: cancellationToken
                        );
                        break;
                    case "BuildRestartingAsync":
                        await _client.BuildRestartingAsync(
                            JsonSerializer.Deserialize<BuildRestartingRequest>(message.RequestContent),
                            cancellationToken: cancellationToken
                        );
                        break;
                    case "InsertPretranslations":

                        {
                            using var call = _client.InsertPretranslations(cancellationToken: cancellationToken);
                            var requests = JsonSerializer.Deserialize<List<InsertPretranslationRequest>>(
                                message.RequestContent
                            );
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
                    case "IncrementTranslationEngineCorpusSizeAsync":
                        await _client.IncrementTranslationEngineCorpusSizeAsync(
                            JsonSerializer.Deserialize<IncrementTranslationEngineCorpusSizeRequest>(
                                message.RequestContent
                            ),
                            cancellationToken: cancellationToken
                        );
                        break;
                    default:
                        await _messages.DeleteAsync(message.Id);
                        _logger.LogWarning(
                            "Unknown method: {message.Method}.  Deleting the message from the list.",
                            message.Method
                        );
                        break;
                }
                await _messages.DeleteAsync(message.Id);
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
                        await FailedMessageAttempt(message, e);
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
        }
        if (allMessagesSuccessfullySent)
        {
            _messagesInOutbox = false;
        }
    }

    async Task PermanentlyFailedMessage(PlatformMessage message, Exception e)
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

    async Task LogFailedAttempt(PlatformMessage message, Exception e)
    {
        // log error
        message.Attempts++;
        await _messages.UpdateAsync(m => m.Id == message.Id, b => b.Set(m => m.Attempts, message.Attempts));
        _logger.LogError(
            e,
            "Attempt {message.Attempts}.  Failed to process message {message.Id}: {message.Method} with content {message.RequestContent}",
            message.Attempts,
            message.Id,
            message.Method,
            message.RequestContent
        );
    }
}
