using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ScriptureRefTests
{
    [TestCase("MAT 1:1", "MAT 1:2", ExpectedResult = -1, Description = "VerseLessThan")]
    [TestCase("MAT 1:1", "MAT 1:1", ExpectedResult = 0, Description = "VerseEqualTo")]
    [TestCase("MAT 1:2", "MAT 1:1", ExpectedResult = 1, Description = "VerseGreaterThan")]
    [TestCase("MAT 1:1-3", "MAT 1:1", ExpectedResult = 1, Description = "MultiVerseExtensionGreaterThan")]
    [TestCase("MAT 1:1", "MAT 1:1-3", ExpectedResult = -1, Description = "MultiVerseExtensionLessThan")]
    [TestCase("MAT 1:1-3", "MAT 1:2", ExpectedResult = -1, Description = "MultiVerseStartLessThan")]
    [TestCase("MAT 1:2", "MAT 1:1-3", ExpectedResult = 1, Description = "MultiVerseStartGreaterThan")]
    [TestCase("MAT 1:0/1:p", "MAT 1:0/2:p", ExpectedResult = -1, Description = "NonVerseLessThan")]
    [TestCase("MAT 1:0/1:p", "MAT 1:0/1:p", ExpectedResult = 0, Description = "NonVerseEqualTo")]
    [TestCase("MAT 1:0/2:p", "MAT 1:0/1:p", ExpectedResult = 1, Description = "NonVerseGreaterThan")]
    [TestCase("MAT 1:0/1:esb", "MAT 1:0/1:esb/1:p", ExpectedResult = -1, Description = "NonVerseParentChild")]
    [TestCase("MAT 1:0/2:esb", "MAT 1:0/1:esb/1:p", ExpectedResult = 1, Description = "NonVerseParentOtherChild")]
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
    [TestCase("MAT 1:0/1:esb", "MAT 1:0/1:esb/1:p", ExpectedResult = -1, Description = "NonVerseParentChild")]
    [TestCase("MAT 1:0/2:esb", "MAT 1:0/1:esb/1:p", ExpectedResult = -1, Description = "NonVerseParentOtherChild")]
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

    [TestCase]
    public void CompareTemplate()
    {
        var refs = new List<ScriptureRef> { ScriptureRef.Parse("MAT 1:2"), ScriptureRef.Parse("MAT 1:1") };
        refs.Sort();
        Assert.That(refs[0].ToString(), Is.EqualTo("MAT 1:1"));
        var dictRefs = new HashSet<ScriptureRef>
        {
            ScriptureRef.Parse("MAT 1:1/1:p"),
            ScriptureRef.Parse("MAT 1:2/1:p"),
            ScriptureRef.Parse("MAT 1:2/1:p")
        };
        Assert.That(dictRefs.Count, Is.EqualTo(2));
    }

    [TestCase("MAT 1:1", "MAT 1:1", ExpectedResult = true, Description = "SameVerse")]
    [TestCase("MAT 1:1", "MAT 1:2", ExpectedResult = false, Description = "DifferentVerse")]
    [TestCase("MAT 1:1", "MAT 1:1-2", ExpectedResult = true, Description = "RangeAndSingleVerse")]
    [TestCase("MAT 1:1-3", "MAT 1:2-4", ExpectedResult = true, Description = "RangeAndRangeTrue")]
    [TestCase("MAT 1:1-3", "MAT 1:4-6", ExpectedResult = false, Description = "RangeAndRangeFalse")]
    [TestCase("MAT 1:0/1:p", "MAT 1:0/1:p", ExpectedResult = true, Description = "NonVerseSameMarker")]
    [TestCase("MAT 1:0/1:p", "MAT 1:0/2:p", ExpectedResult = false, Description = "NonVerseDifferentPosition")]
    [TestCase("MAT 1:0/1:esb", "MAT 1:0/1:esb/1:p", ExpectedResult = false, Description = "NonVerseParentChild")]
    [TestCase("MAT 1:0/1:p/2:esb", "MAT 1:0/2:esb/1:p", ExpectedResult = false, Description = "NonVerseParentChild")]
    public bool Overlaps(string ref1Str, string ref2Str)
    {
        var ref1 = ScriptureRef.Parse(ref1Str);
        var ref2 = ScriptureRef.Parse(ref2Str);
        bool overlaps = ref1.Overlaps(ref2);
        return overlaps;
    }

    [TestCase]
    public void IsEqualTo()
    {
        var ref1 = ScriptureRef.Parse("MAT 1:1/1:p");
        var ref1dup = ScriptureRef.Parse("MAT 1:1/1:p");
        var ref2 = ScriptureRef.Parse("MAT 1:2/1:p");
        var obj1 = "A different type";
        Assert.Multiple(() =>
        {
            Assert.That(ref1.Equals(ref1dup));
            Assert.That(!ref1.Equals(ref2));
            Assert.That(!ref1.Equals(obj1));
        });
        Assert.Throws<ArgumentException>(() => ref1.CompareTo(obj1));
    }
}
