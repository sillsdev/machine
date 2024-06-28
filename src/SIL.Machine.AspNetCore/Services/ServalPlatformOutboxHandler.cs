using Serval.Translation.V1;

namespace SIL.Machine.AspNetCore.Services;

public enum ServalPlatformMessageMethod
{
    BuildStarted,
    BuildCompleted,
    BuildCanceled,
    BuildFaulted,
    BuildRestarting,
    InsertPretranslations,
    IncrementTranslationEngineCorpusSize
}

public class ServalPlatformOutboxHandler(
    TranslationPlatformApi.TranslationPlatformApiClient client,
    ISharedFileService sharedFileService,
    ILogger<ServalPlatformOutboxHandler> logger
) : IOutboxMessageHandler
{
    private readonly TranslationPlatformApi.TranslationPlatformApiClient _client = client;
    private readonly ISharedFileService _sharedFileService = sharedFileService;
    private readonly ILogger<ServalPlatformOutboxHandler> _logger = logger;
    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly string _name = typeof(ServalPlatformMessageMethod).ToString();
    public string Name => _name;

    public async Task SendMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ServalPlatformMessageMethod messageType = Enum.Parse<ServalPlatformMessageMethod>(message.Method);
        switch (messageType)
        {
            case ServalPlatformMessageMethod.BuildStarted:
                await _client.BuildStartedAsync(
                    JsonSerializer.Deserialize<BuildStartedRequest>(message.RequestContent!),
                    cancellationToken: cancellationToken
                );
                break;
            case ServalPlatformMessageMethod.BuildCompleted:
                await _client.BuildCompletedAsync(
                    JsonSerializer.Deserialize<BuildCompletedRequest>(message.RequestContent!),
                    cancellationToken: cancellationToken
                );
                break;
            case ServalPlatformMessageMethod.BuildCanceled:
                await _client.BuildCanceledAsync(
                    JsonSerializer.Deserialize<BuildCanceledRequest>(message.RequestContent!),
                    cancellationToken: cancellationToken
                );
                break;
            case ServalPlatformMessageMethod.BuildFaulted:
                await _client.BuildFaultedAsync(
                    JsonSerializer.Deserialize<BuildFaultedRequest>(message.RequestContent!),
                    cancellationToken: cancellationToken
                );
                break;
            case ServalPlatformMessageMethod.BuildRestarting:
                await _client.BuildRestartingAsync(
                    JsonSerializer.Deserialize<BuildRestartingRequest>(message.RequestContent!),
                    cancellationToken: cancellationToken
                );
                break;
            case ServalPlatformMessageMethod.InsertPretranslations:

                {
                    using Stream targetPretranslateStream = await _sharedFileService.OpenReadAsync(
                        $"outbox/{message.Id}",
                        cancellationToken
                    );
                    IAsyncEnumerable<Pretranslation> pretranslations = JsonSerializer
                        .DeserializeAsyncEnumerable<Pretranslation>(
                            targetPretranslateStream,
                            JsonSerializerOptions,
                            cancellationToken
                        )
                        .OfType<Pretranslation>();

                    using var call = _client.InsertPretranslations(cancellationToken: cancellationToken);
                    await foreach (Pretranslation? pretranslation in pretranslations)
                    {
                        await call.RequestStream.WriteAsync(
                            new InsertPretranslationRequest
                            {
                                EngineId = message.RequestContent!,
                                CorpusId = pretranslation.CorpusId,
                                TextId = pretranslation.TextId,
                                Refs = { pretranslation.Refs },
                                Translation = pretranslation.Translation
                            },
                            cancellationToken
                        );
                    }
                    await call.RequestStream.CompleteAsync();
                    await call;
                }
                break;
            case ServalPlatformMessageMethod.IncrementTranslationEngineCorpusSize:
                await _client.IncrementTranslationEngineCorpusSizeAsync(
                    JsonSerializer.Deserialize<IncrementTranslationEngineCorpusSizeRequest>(message.RequestContent!),
                    cancellationToken: cancellationToken
                );
                break;
            default:
                _logger.LogWarning(
                    "Unknown method: {message.Method}.  Deleting the message from the list.",
                    message.Method.ToString()
                );
                break;
        }
    }

    public async Task CleanupMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        if (await _sharedFileService.ExistsAsync($"outbox/{message.Id}", cancellationToken))
            await _sharedFileService.DeleteAsync($"outbox/{message.Id}", cancellationToken);
    }
}
