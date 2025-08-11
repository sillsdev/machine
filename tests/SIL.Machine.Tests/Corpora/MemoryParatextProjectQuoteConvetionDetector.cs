using System.Text;

namespace SIL.Machine.Corpora;

public class MemoryParatextProjectQuoteConventionDetector(
    ParatextProjectSettings settings,
    IDictionary<string, string> files
) : ParatextProjectQuoteConventionDetector(settings)
{
    public IDictionary<string, string> Files { get; } = files;

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
