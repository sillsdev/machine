namespace SIL.Machine.AspNetCore.Models;

public enum FileFormat
{
    Text = 0,
    Paratext = 1
}

public class CorpusFile
{
    public string Location { get; set; } = default!;
    public FileFormat Format { get; set; }
    public string TextId { get; set; } = default!;
}
