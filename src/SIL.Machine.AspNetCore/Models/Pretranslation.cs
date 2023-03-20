namespace SIL.Machine.AspNetCore.Models;

public class Pretranslation
{
    public string CorpusId { get; set; } = default!;
    public string TextId { get; set; } = default!;
    public List<string> Refs { get; set; } = new List<string>();
    public string Translation { get; set; } = default!;
}
