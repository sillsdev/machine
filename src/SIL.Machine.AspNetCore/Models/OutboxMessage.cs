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
    public required OutboxMessageMethod Method { get; init; }
    public required string RequestContent { get; init; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public int Attempts { get; set; } = 0;
}
