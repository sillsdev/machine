using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Scripture;

[TestFixture]
public class ScriptureRangeParserTests
{
    [TestCaseSource(nameof(GetCases))]
    public void GetChapters(string rangeString, Dictionary<string, List<int>> expectedOutput, bool throwsException)
    {
        var parser = new ScriptureRangeParser();
        if (!throwsException)
        {
            Assert.That(parser.GetChapters(rangeString), Is.EquivalentTo(expectedOutput));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
            {
                parser.GetChapters(rangeString);
            });
        }
    }

    [TestCaseSource(nameof(GetCases))]
    public void TryGetChapters(string rangeString, Dictionary<string, List<int>> expectedOutput, bool throwsException)
    {
        var parser = new ScriptureRangeParser();
        bool actual = parser.TryGetChapters(rangeString, out Dictionary<string, List<int>> chapters);
        if (!throwsException)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(chapters, Is.EquivalentTo(expectedOutput));
                Assert.That(actual, Is.True);
            }
        }
        else
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(chapters, Is.Null);
                Assert.That(actual, Is.False);
            }
        }
    }

    private static IEnumerable<TestCaseData> GetCases()
    {
        yield return new TestCaseData("MAL", new Dictionary<string, List<int>> { { "MAL", [] } }, false);
        yield return new TestCaseData("PS2", new Dictionary<string, List<int>> { { "PS2", [] } }, false);
        yield return new TestCaseData(
            "GEN,EXO",
            new Dictionary<string, List<int>> { { "GEN", [] }, { "EXO", [] } },
            false
        );
        yield return new TestCaseData(
            "1JN,2JN",
            new Dictionary<string, List<int>> { { "1JN", [] }, { "2JN", [] } },
            false
        );
        yield return new TestCaseData(
            "OT",
            Enumerable.Range(1, 39).Select(i => (Canon.BookNumberToId(i), new List<int>())).ToDictionary(),
            false
        );
        yield return new TestCaseData(
            "NT",
            Enumerable.Range(40, 27).Select(i => (Canon.BookNumberToId(i), new List<int>())).ToDictionary(),
            false
        );
        yield return new TestCaseData(
            "NT,OT",
            Enumerable.Range(1, 66).Select(i => (Canon.BookNumberToId(i), new List<int>())).ToDictionary(),
            false
        );
        yield return new TestCaseData(
            "MAT;MRK",
            new Dictionary<string, List<int>> { { "MAT", [] }, { "MRK", [] } },
            false
        );
        yield return new TestCaseData(
            "MAT; MRK",
            new Dictionary<string, List<int>> { { "MAT", [] }, { "MRK", [] } },
            false
        );
        yield return new TestCaseData("MAT1,2,3", new Dictionary<string, List<int>> { { "MAT", [1, 2, 3] } }, false);
        yield return new TestCaseData("MAT1, 2, 3", new Dictionary<string, List<int>> { { "MAT", [1, 2, 3] } }, false);
        yield return new TestCaseData(
            "MAT-LUK",
            new Dictionary<string, List<int>>
            {
                { "MAT", [] },
                { "MRK", [] },
                { "LUK", [] },
            },
            false
        );
        yield return new TestCaseData(
            "MAT1,2,3;MAT-LUK",
            new Dictionary<string, List<int>>
            {
                { "MAT", [] },
                { "MRK", [] },
                { "LUK", [] },
            },
            false
        );
        yield return new TestCaseData(
            "2JN-3JN;EXO1,8,3-5;GEN",
            new Dictionary<string, List<int>>
            {
                { "GEN", [] },
                { "EXO", [1, 3, 4, 5, 8] },
                { "2JN", [] },
                { "3JN", [] },
            },
            false
        );
        yield return new TestCaseData(
            "1JN 1;1JN 2;1JN 3-5",
            new Dictionary<string, List<int>> { { "1JN", [] } },
            false
        );
        yield return new TestCaseData(
            "MAT-ROM;-ACT4-28",
            new Dictionary<string, List<int>>
            {
                { "MAT", [] },
                { "MRK", [] },
                { "LUK", [] },
                { "JHN", [] },
                { "ACT", [1, 2, 3] },
                { "ROM", new List<int>() },
            },
            false
        );
        yield return new TestCaseData("2JN;-2JN 1", new Dictionary<string, List<int>>(), false);
        yield return new TestCaseData(
            "NT;OT;-MRK;-EXO",
            Enumerable
                .Range(1, 66)
                .Where(i => i != 2 && i != 41)
                .Select(i => (Canon.BookNumberToId(i), new List<int>()))
                .ToDictionary(),
            false
        );
        yield return new TestCaseData(
            "NT;-MAT3-5,17;-REV21,22",
            Enumerable
                .Range(40, 27)
                .Select(i =>
                {
                    return i switch
                    {
                        40 => (
                            Canon.BookNumberToId(i),
                            Enumerable.Range(1, 28).Where(c => c is not (3 or 4 or 5 or 17)).ToList()
                        ),
                        66 => (Canon.BookNumberToId(i), [.. Enumerable.Range(1, 20)]),
                        _ => (Canon.BookNumberToId(i), []),
                    };
                })
                .ToDictionary(),
            false
        );
        yield return new TestCaseData("MAT-JHN;-MAT-LUK", new Dictionary<string, List<int>> { { "JHN", [] } }, false);
        yield return new TestCaseData("", new Dictionary<string, List<int>>(), false);

        //*Throw exceptions
        yield return new TestCaseData("MAT3-1", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MRK-MAT", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MRK;-MRK10-3", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT0-10", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT-FLUM", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("-MAT-FLUM", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("ABC", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT-ABC", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("NT;-ABC-LUK", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT 500", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT 1-500", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT;-MAT 300-500", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("-MRK", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("-MRK 1", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MRK 2-5;-MRK 1-4", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MRK 2-5;-MRK 6", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("OT;-MRK-LUK", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("NT;OT;-ABC", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT;-ABC 1", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("NT,OT,-MRK,-EXO", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("OT,MAT1", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("OT,MAT-LUK", new Dictionary<string, List<int>>(), true);
    }
}
