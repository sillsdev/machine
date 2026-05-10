using System.Text;

namespace SIL.Machine.Corpora;

public class MemoryParatextProjectFileHandler(IDictionary<string, string>? files = null) : IParatextProjectFileHandler
{
    public IDictionary<string, string> Files { get; } = files ?? new Dictionary<string, string>();

    public UsfmStylesheet CreateStylesheet(string fileName) =>
        fileName is "usfm.sty" or "usfm_sb.sty" ? new UsfmStylesheet(fileName) : throw new NotImplementedException();

    public bool Exists(string fileName)
    {
        return Files.ContainsKey(fileName);
    }

    public string? Find(string extension) => Files.Keys.FirstOrDefault(item => item.EndsWith(extension));

    public Stream? Open(string fileName)
    {
        if (!Files.TryGetValue(fileName, out string? contents))
            return null;
        return new MemoryStream(Encoding.UTF8.GetBytes(contents));
    }
}
