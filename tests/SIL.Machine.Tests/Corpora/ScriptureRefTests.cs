using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ScriptureRefTests
{
    [TestCase("MAT 1:1", "MAT 1:2", ExpectedResult = -1, Description = "VerseLessThan")]
    [TestCase("MAT 1:1", "MAT 1:1", ExpectedResult = 0, Description = "VerseEqualTo")]
    [TestCase("MAT 1:2", "MAT 1:1", ExpectedResult = 1, Description = "VerseGreaterThan")]
    [TestCase("MAT 1:0/1:p", "MAT 1:0/2:p", ExpectedResult = -1, Description = "NonVerseLessThan")]
    [TestCase("MAT 1:0/1:p", "MAT 1:0/1:p", ExpectedResult = 0, Description = "NonVerseEqualTo")]
    [TestCase("MAT 1:0/2:p", "MAT 1:0/1:p", ExpectedResult = 1, Description = "NonVerseGreaterThan")]
    [TestCase("MAT 1:0/1:esb", "MAT 1:0/1:esb/1:p", ExpectedResult = -1, Description = "NonVerseParentChild")]
    public int CompareTo_Strict(string ref1Str, string ref2Str)
    {
        var ref1 = ScriptureRef.Parse(ref1Str);
        var ref2 = ScriptureRef.Parse(ref2Str);

        int result = ref1.CompareTo(ref2);

        if (result < 0)
            result = -1;
        else if (result > 0)
            result = 1;
        return result;
    }

    [TestCase("MAT 1:1", "MAT 1:2", ExpectedResult = -1, Description = "VerseLessThan")]
    [TestCase("MAT 1:1", "MAT 1:1", ExpectedResult = 0, Description = "VerseEqualTo")]
    [TestCase("MAT 1:2", "MAT 1:1", ExpectedResult = 1, Description = "VerseGreaterThan")]
    [TestCase("MAT 1:0/1:p", "MAT 1:0/2:p", ExpectedResult = 0, Description = "NonVerseSameMarkerDifferentPosition")]
    [TestCase("MAT 1:0/2:esb", "MAT 1:0/1:esb/1:p", ExpectedResult = -1, Description = "NonVerseParentChild")]
    public int CompareTo_Relaxed(string ref1Str, string ref2Str)
    {
        var ref1 = ScriptureRef.Parse(ref1Str);
        var ref2 = ScriptureRef.Parse(ref2Str);

        int result = ref1.CompareTo(ref2, strict: false);

        if (result < 0)
            result = -1;
        else if (result > 0)
            result = 1;
        return result;
    }
}
