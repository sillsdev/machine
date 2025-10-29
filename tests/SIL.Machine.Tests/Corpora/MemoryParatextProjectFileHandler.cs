using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

public class MemoryParatextProjectFileHandler(IDictionary<string, string>? files = null) : IParatextProjectFileHandler
{
    public IDictionary<string, string> Files { get; } = files ?? new Dictionary<string, string>();

    public UsfmStylesheet CreateStylesheet(string fileName)
    {
        throw new NotImplementedException();
    }

    public bool Exists(string fileName)
    {
        return Files.ContainsKey(fileName);
    }

    public string Find(string extension)
    {
        throw new NotImplementedException();
    }

    public Stream? Open(string fileName)
    {
        if (!Files.TryGetValue(fileName, out string? contents))
            return null;
        return new MemoryStream(Encoding.UTF8.GetBytes(contents));
    }

    public class DefaultParatextProjectSettings(
        string name = "Test",
        string fullName = "TestProject",
        Encoding? encoding = null,
        ScrVers? versification = null,
        UsfmStylesheet? stylesheet = null,
        string fileNamePrefix = "",
        string fileNameForm = "41MAT",
        string fileNameSuffix = "Test.SFM",
        string biblicalTermsListType = "Project",
        string biblicalTermsProjectName = "Test",
        string biblicalTermsFileName = "ProjectBiblicalTerms.xml",
        string languageCode = "en"
    )
        : ParatextProjectSettings(
            name,
            fullName,
            encoding ?? Encoding.UTF8,
            versification ?? ScrVers.English,
            stylesheet ?? new UsfmStylesheet("usfm.sty"),
            fileNamePrefix,
            fileNameForm,
            fileNameSuffix,
            biblicalTermsListType,
            biblicalTermsProjectName,
            biblicalTermsFileName,
            languageCode
        ) { }
}
