using NUnit.Framework;

namespace SIL.Machine.FeatureModel;

// Covers the thread-static pool of visited-set collections that FeatureStruct.ValueEquals/Freeze/
// GetFrozenHashCode use to guard their recursive walk. The pool is reused across top-level calls on a thread;
// a re-entrant top-level call (one made from inside another comparison that is still on the stack) must fall
// back to fresh, unpooled allocations rather than corrupt the outer call's in-progress state, and must still
// produce a correct answer.
[TestFixture]
public class FeatureStructPooledVisitedSetsTests
{
    // A StringFeatureValue whose ValueEquals override makes a nested, unrelated top-level
    // FeatureStruct.ValueEquals call before delegating to the normal string comparison. Since
    // SimpleFeatureValue.ValueEqualsImpl dispatches to this override from inside the outer FeatureStruct
    // comparison's walk, the nested call is forced to run while the outer call still holds the pool.
    private sealed class ReentrantProbeValue(string value, FeatureStruct nestedA, FeatureStruct nestedB)
        : StringFeatureValue(value)
    {
        public bool NestedCallMade { get; private set; }
        public bool NestedResult { get; private set; }

        public override bool ValueEquals(SimpleFeatureValue other)
        {
            NestedCallMade = true;
            NestedResult = nestedA.ValueEquals(nestedB);
            return base.ValueEquals(other);
        }
    }

    [Test]
    public void ValueEquals_ReentrantTopLevelCall_FallsBackAndIsCorrect()
    {
        var strFeature = new StringFeature("s");
        var nestedA = new FeatureStruct();
        nestedA.AddValue(strFeature, new StringFeatureValue("abc"));
        var nestedB = new FeatureStruct();
        nestedB.AddValue(strFeature, new StringFeatureValue("abc"));

        var probeFeature = new StringFeature("probe");
        var probe = new ReentrantProbeValue("v", nestedA, nestedB);

        var fsA = new FeatureStruct();
        fsA.AddValue(probeFeature, probe);

        var fsB = new FeatureStruct();
        fsB.AddValue(probeFeature, new StringFeatureValue("v"));

        bool result = fsA.ValueEquals(fsB);

        Assert.That(probe.NestedCallMade, Is.True, "the leaf comparison should have invoked the probe override");
        Assert.That(
            probe.NestedResult,
            Is.True,
            "the nested top-level ValueEquals call, forced onto the fallback path, must still return the correct answer"
        );
        Assert.That(result, Is.True);
    }

    [Test]
    public void ValueEquals_ReentrantTopLevelCall_OuterComparisonStillCorrectWhenNestedDiffers()
    {
        var strFeature = new StringFeature("s");
        var nestedA = new FeatureStruct();
        nestedA.AddValue(strFeature, new StringFeatureValue("abc"));
        var nestedB = new FeatureStruct();
        nestedB.AddValue(strFeature, new StringFeatureValue("xyz"));

        var probeFeature = new StringFeature("probe");
        var probe = new ReentrantProbeValue("v", nestedA, nestedB);

        var fsA = new FeatureStruct();
        fsA.AddValue(probeFeature, probe);

        var fsB = new FeatureStruct();
        fsB.AddValue(probeFeature, new StringFeatureValue("v"));

        bool result = fsA.ValueEquals(fsB);

        Assert.That(probe.NestedResult, Is.False);
        // the outer comparison is only about the probe's own string value ("v" == "v"), which is unaffected by
        // the nested comparison's result.
        Assert.That(result, Is.True);
    }

    [Test]
    public void Freeze_AfterPooledUse_PoolIsClearedAndReusableForUnrelatedStructs()
    {
        var strFeature = new StringFeature("s");

        var fs1 = new FeatureStruct();
        fs1.AddValue(strFeature, new StringFeatureValue("abc"));
        fs1.Freeze();

        var fs2 = new FeatureStruct();
        fs2.AddValue(strFeature, new StringFeatureValue("def"));
        fs2.Freeze();

        // Two independent Freeze calls sharing the same thread-static pool must not leak visited state between
        // them: fs2 must not be considered "already visited" just because fs1 used the pool first.
        Assert.That(fs1.GetFrozenHashCode(), Is.Not.EqualTo(fs2.GetFrozenHashCode()));

        var fs3 = new FeatureStruct();
        fs3.AddValue(strFeature, new StringFeatureValue("abc"));
        fs3.Freeze();

        Assert.That(fs1.GetFrozenHashCode(), Is.EqualTo(fs3.GetFrozenHashCode()));
        Assert.That(fs1.ValueEquals(fs3), Is.True);
    }
}
