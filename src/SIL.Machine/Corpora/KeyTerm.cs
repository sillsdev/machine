using System.Collections.Generic;
using System.Linq;
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
        IEnumerable<string> renderings,
        IEnumerable<VerseRef> references,
        IEnumerable<string> renderingsPatterns
    )
    {
        Id = id;
        Category = category;
        Domain = domain;
        Renderings = renderings.ToArray();
        References = references.ToArray();
        RenderingsPatterns = renderingsPatterns.ToArray();
    }
}
