namespace SIL.Machine.AspNetCore.Models;

public record OutboxMessage : IEntity
{
    public string Id { get; set; } = "";
    public int Revision { get; set; } = 1;
    public required int Index { get; set; }
    public required string OutboxName { get; set; }
    public required string Method { get; set; }
    public required string GroupId { get; set; }
    public required string? RequestContent { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public int Attempts { get; set; } = 0;
}
