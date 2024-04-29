namespace SIL.Machine.AspNetCore.Models;

public record Sequence : IEntity
{
    public string Id { get; set; } = "";

    public int Revision { get; set; }

    public string Context { get; set; } = "";
    public int CurrentIndex { get; set; }

    public static string IndexToObjectIdString(int value)
    {
        return value.ToString("x24");
    }
}
