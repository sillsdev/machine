namespace SIL.Machine.WebApi.Services;

public interface IWebhookService
{
    Task<IEnumerable<Webhook>> GetAllAsync(string owner);
    Task<Webhook?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task CreateAsync(Webhook hook);
    Task<bool> DeleteAsync(string id);

    Task SendEventAsync<T>(WebhookEvent webhookEvent, string owner, T resource);
}
