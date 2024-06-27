namespace SIL.Machine.AspNetCore.Models;

public record Outbox : IEntity
{
    public string Id { get; set; } = "";

    public int Revision { get; set; }

    public required string Name { get; init; } = null!;
    public required int CurrentIndex { get; set; }

    public static async Task<Outbox> GetOutboxNextIndexAsync(
        IRepository<Outbox> indexRepository,
        string outboxName,
        CancellationToken cancellationToken
    )
    {
        Outbox outbox = (
            await indexRepository.UpdateAsync(
                i => i.Name == outboxName,
                i => i.Inc(b => b.CurrentIndex, 1),
                upsert: true,
                cancellationToken: cancellationToken
            )
        )!;
        return outbox;
    }
}
