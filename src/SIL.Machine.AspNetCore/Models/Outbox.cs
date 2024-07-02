namespace SIL.Machine.AspNetCore.Models;

public record Outbox : IEntity
{
    public string Id { get; set; } = "";

    public int Revision { get; set; }

    public int CurrentIndex { get; init; }
}
