namespace SIL.Machine.AspNetCore.Models;

public record OutboxMessage : IEntity
{
    public string Id { get; set; } = "";
    public int Revision { get; set; } = 1;
    public required int Index { get; init; }
    public required string OutboxRef { get; init; }
    public required string Method { get; init; }
    public required string GroupId { get; init; }
    public string? Content { get; init; }
    public required bool HasContentStream { get; init; }
    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;
    public int Attempts { get; init; }
}
