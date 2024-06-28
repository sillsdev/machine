namespace SIL.Machine.AspNetCore.Configuration;

public class MessageOutboxOptions
{
    public const string Key = "MessageOutbox";

    public string DataDir { get; set; } = "outbox";
    public TimeSpan MessageExpirationTimeout { get; set; } = TimeSpan.FromHours(48);
}
