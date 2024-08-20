using System.Text;

namespace SIL.Machine.Corpora;

public class MemoryParatextProjectTermsCorpus : ParatextTermsCorpusBase
{
    public Dictionary<string, string> Files { get; }

    public MemoryParatextProjectTermsCorpus(
        ParatextProjectSettings settings,
        IEnumerable<string> termCategories,
        Dictionary<string, string> files,
        bool preferTermsLocalization = false
    )
    {
        Files = files;
        AddTexts(settings, termCategories, preferTermsLocalization);
    }

    protected override bool Exists(string fileName)
    {
        return Files.ContainsKey(fileName);
    }

    protected override Stream? Open(string fileName)
    {
        if (!Files.TryGetValue(fileName, out string? contents))
            return null;
        return new MemoryStream(Encoding.UTF8.GetBytes(contents));
    }
}
