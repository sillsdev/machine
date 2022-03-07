namespace SIL.Machine.WebApi.Services;

public interface IWebhookService
{
	Task TriggerEventAsync<T>(WebhookEvent webhookEvent, string owner, T resource);
}
