namespace SIL.Machine.AspNetCore.Models;

public record Lock
{
    public required string Id { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public required string HostId { get; init; }
}
