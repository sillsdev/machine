using System.Collections.Generic;
using SIL.Scripture;

public class KeyTerm
{
    public string Id { get; }
    public string Category { get; }
    public string Domain { get; }
    public IReadOnlyList<string> Renderings { get; }
    public IReadOnlyList<VerseRef> References { get; }
    public IReadOnlyList<string> RenderingsPatterns { get; }

    public KeyTerm(
        string id,
        string category,
        string domain,
        IReadOnlyList<string> renderings,
        IReadOnlyList<VerseRef> references,
        IReadOnlyList<string> renderingsPatterns
    )
    {
        Id = id;
        Category = category;
        Domain = domain;
        Renderings = renderings;
        References = references;
        RenderingsPatterns = renderingsPatterns;
    }
}
