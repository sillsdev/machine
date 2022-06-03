namespace SIL.Machine.WebApi.Models;

public class Webhook : IOwnedEntity
{
    public string Id { get; set; } = default!;
    public int Revision { get; set; } = 1;
    public string Owner { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string Secret { get; set; } = default!;
    public List<WebhookEvent> Events { get; set; } = new List<WebhookEvent>();
}
