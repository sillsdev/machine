using System.IO.Compression;
using NUnit.Framework.Constraints;

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
    public static readonly string UsfmTargetProjectPath = Path.Combine(TestDataPath, "usfm", "target");
    public static readonly string UsfmTargetProjectZipPath = Path.Combine(TestDataPath, "project", "target");
    public static readonly string UsfmSourceProjectPath = Path.Combine(TestDataPath, "usfm", "source");
    public static readonly string UsfmSourceProjectZipPath = Path.Combine(TestDataPath, "project", "source");
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

    public static EqualUsingConstraint<string> IgnoreLineEndings(this EqualStringConstraint constraint)
    {
        return constraint.Using(new IgnoreLineEndingsStringComparer());
    }

    private sealed class IgnoreLineEndingsStringComparer : StringComparer
    {
        public override int Compare(string? x, string? y)
        {
            return string.Compare(x?.ReplaceLineEndings(), y?.ReplaceLineEndings(), StringComparison.InvariantCulture);
        }

        public override bool Equals(string? x, string? y) =>
            string.Equals(x?.ReplaceLineEndings(), y?.ReplaceLineEndings(), StringComparison.InvariantCulture);

        public override int GetHashCode(string obj) => obj.ReplaceLineEndings().GetHashCode();
    }
}
