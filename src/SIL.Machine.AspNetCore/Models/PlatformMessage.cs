namespace SIL.Machine.AspNetCore.Models;

public record PlatformMessage : IEntity
{
    public string Id { get; set; } = "";
    public int Revision { get; set; } = 1;
    public required string Method { get; init; }
    public required string RequestContent { get; init; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public int Attempts { get; set; } = 0;
}
