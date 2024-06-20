namespace SIL.Machine.AspNetCore.Configuration;

public class MessageOutboxOptions
{
    public const string Key = "MessageOutbox";

    public int MessageExpirationInHours { get; set; } = 48;
}
