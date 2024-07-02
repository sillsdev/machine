namespace SIL.Machine.AspNetCore.Configuration;

public class MessageOutboxOptions
{
    public const string Key = "MessageOutbox";

    public string OutboxDir { get; set; } = "outbox";
    public TimeSpan MessageExpirationTimeout { get; set; } = TimeSpan.FromHours(48);
}
