namespace SIL.Machine.WebApi.Models;

public class DataFile
{
    public string Id { get; set; } = default!;
    public string LanguageTag { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Filename { get; set; } = default!;
    public string? TextId { get; set; }
}
