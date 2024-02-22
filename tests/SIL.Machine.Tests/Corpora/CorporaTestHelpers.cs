using System.IO.Compression;

namespace SIL.Machine.Corpora;

internal static class CorporaTestHelpers
{
    public static readonly string TestDataPath = Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "Corpora",
        "TestData"
    );
    public static readonly string UsfmTestProjectPath = Path.Combine(TestDataPath, "usfm", "Tes");
    public static readonly string UsxTestProjectPath = Path.Combine(TestDataPath, "usx", "Tes");
    public static readonly string TextTestProjectPath = Path.Combine(TestDataPath, "txt");

    public static string CreateTestDblBundle()
    {
        string path = Path.Combine(Path.GetTempPath(), "Tes.zip");
        if (File.Exists(path))
            File.Delete(path);
        ZipFile.CreateFromDirectory(UsxTestProjectPath, path);
        return path;
    }

    public static string CreateTestParatextBackup()
    {
        string path = Path.Combine(Path.GetTempPath(), "Tes.zip");
        if (File.Exists(path))
            File.Delete(path);
        ZipFile.CreateFromDirectory(UsfmTestProjectPath, path);
        return path;
    }
}
