namespace SIL.Machine.WebApi.Services;

public interface IWebhookService
{
    Task SendEventAsync<T>(WebhookEvent webhookEvent, string owner, T resource);
}
