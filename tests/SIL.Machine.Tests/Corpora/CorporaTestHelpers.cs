using System.IO.Compression;
using NUnit.Framework.Constraints;
using SIL.Scripture;

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
    public static readonly string UsfmTargetCustomVrsPath = Path.Combine(TestDataPath, "usfm", "target", "custom.vrs");
    public static readonly string UsfmSourceProjectPath = Path.Combine(TestDataPath, "usfm", "source");
    public static readonly string UsxTestProjectPath = Path.Combine(TestDataPath, "usx", "Tes");
    public static readonly string TextTestProjectPath = Path.Combine(TestDataPath, "txt");
    public static readonly string DeuterocanonicalsSourcePath = Path.Combine(
        TestDataPath,
        "deuterocanonicals",
        "source"
    );
    public static readonly string DeuterocanonicalsTargetPath = Path.Combine(
        TestDataPath,
        "deuterocanonicals",
        "target"
    );

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

    public static EqualConstraint IgnoreLineEndings(this EqualConstraint constraint)
    {
        return constraint.Using<string>(
            (actual, expected) => actual.ReplaceLineEndings() == expected.ReplaceLineEndings()
        );
    }

    /// <summary>
    /// Sets up and returns the source corpus.
    /// </summary>
    /// <returns>The source corpus.</returns>
    public static ParatextTextCorpus GetDeuterocanonicalSourceCorpus()
    {
        return new ParatextTextCorpus(CorporaTestHelpers.DeuterocanonicalsSourcePath, includeAllText: true);
    }

    /// <summary>
    /// Sets up and returns the target corpus.
    /// </summary>
    /// <returns>The target corpus.</returns>
    public static ParatextTextCorpus GetDeuterocanonicalTargetCorpus()
    {
        return new ParatextTextCorpus(CorporaTestHelpers.DeuterocanonicalsTargetPath, includeAllText: true);
    }

    /// <summary>
    /// Expands a hyphenated verse range (e.g., "S3Y 1:1-29") into individual verses.
    /// </summary>
    public static IEnumerable<ScriptureRef> ExpandVerseRange(string verseRange, ScrVers versification)
    {
        var parts = verseRange.Split(':');
        var bookAndChapter = parts[0].Trim();
        var verses = parts[1];

        if (verses.Contains('-'))
        {
            var rangeParts = verses.Split('-').Select(int.Parse).ToArray();
            var startVerse = rangeParts[0];
            var endVerse = rangeParts[1];

            for (int verse = startVerse; verse <= endVerse; verse++)
            {
                yield return ScriptureRef.Parse($"{bookAndChapter}:{verse}", versification);
            }
        }
        else
        {
            yield return ScriptureRef.Parse(verseRange, versification);
        }
    }

    public static Dictionary<string, string> ExpandVerseMappings(Dictionary<string, string> mappings)
    {
        var expandedMappings = new Dictionary<string, string>();

        foreach (var mapping in mappings)
        {
            var sourceParts = ParseRange(mapping.Key);
            var targetParts = ParseRange(mapping.Value);

            // Check if either source or target is a single verse
            if (sourceParts.IsSingleVerse && targetParts.IsSingleVerse)
            {
                expandedMappings[mapping.Key] = mapping.Value;
                continue;
            }

            int sourceVerseCount = sourceParts.EndVerse - sourceParts.StartVerse + 1;
            int targetVerseCount = targetParts.EndVerse - targetParts.StartVerse + 1;

            if (sourceVerseCount != targetVerseCount)
            {
                throw new InvalidOperationException(
                    "Source and target verse ranges must have the same number of verses."
                );
            }

            for (int i = 0; i < sourceVerseCount; i++)
            {
                string sourceVerse = $"{sourceParts.Book} {sourceParts.Chapter}:{sourceParts.StartVerse + i}";
                string targetVerse = $"{targetParts.Book} {targetParts.Chapter}:{targetParts.StartVerse + i}";

                expandedMappings[sourceVerse] = targetVerse;
            }
        }

        return expandedMappings;
    }

    private static (string Book, int Chapter, int StartVerse, int EndVerse, bool IsSingleVerse) ParseRange(string range)
    {
        var parts = range.Split(' ');
        var book = parts[0];

        var chapterAndVerses = parts[1].Split(':');
        int chapter = int.Parse(chapterAndVerses[0]);

        var verseRange = chapterAndVerses[1].Split('-');

        int startVerse = int.Parse(verseRange[0]);
        int endVerse = verseRange.Length > 1 ? int.Parse(verseRange[1]) : startVerse;

        bool isSingleVerse = startVerse == endVerse;

        return (book, chapter, startVerse, endVerse, isSingleVerse);
    }

    /// <summary>
    /// Removes unwanted characters in a corpus string.
    /// </summary>
    public static string CleanString(string input, string[] unwanted)
    {
        foreach (var item in unwanted)
        {
            input = input.Replace(item, "").Trim();
        }
        return input;
    }
}
