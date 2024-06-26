namespace SIL.Machine.AspNetCore.Models;

public enum OutboxMessageMethod
{
    BuildStarted,
    BuildCompleted,
    BuildCanceled,
    BuildFaulted,
    BuildRestarting,
    InsertPretranslations,
    IncrementTranslationEngineCorpusSize
}

public record OutboxMessage : IEntity
{
    public string Id { get; set; } = "";
    public int Revision { get; set; } = 1;
    public required string SortableIndex { get; set; }
    public required OutboxMessageMethod Method { get; set; }
    public required string GroupId { get; set; }
    public required string? RequestContent { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public int Attempts { get; set; } = 0;
}
