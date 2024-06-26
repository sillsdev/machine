namespace SIL.Machine.AspNetCore.Models;

public record SortableIndex : IEntity
{
    public string Id { get; set; } = "";

    public int Revision { get; set; }

    public string Context { get; set; } = "";
    public int CurrentIndex { get; set; }

    public static string IndexToObjectIdString(int value)
    {
        return value.ToString("x24");
    }

    public static async Task<string> GetSortableIndexAsync(
        IRepository<SortableIndex> indexRepository,
        string context,
        CancellationToken cancellationToken
    )
    {
        SortableIndex outboxIndex = (
            await indexRepository.UpdateAsync(
                i => i.Context == context,
                i => i.Inc(b => b.CurrentIndex, 1),
                upsert: true,
                cancellationToken: cancellationToken
            )
        )!;
        return IndexToObjectIdString(outboxIndex.CurrentIndex);
    }
}
