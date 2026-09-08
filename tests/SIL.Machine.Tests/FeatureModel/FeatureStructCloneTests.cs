using NUnit.Framework;

namespace SIL.Machine.FeatureModel;

// Covers FeatureStruct.Clone()'s two paths: skipping the copies map entirely for a frozen, tree-shaped source
// (nothing to share), and the pooled copies-map path (with a fresh-allocation fallback for re-entrant top-level
// Clone() calls) for everything else -- shared leaves, nested structures, cycles, and unfrozen sources.
[TestFixture]
public class FeatureStructCloneTests
{
    [Test]
    public void Clone_FrozenTreeStruct_TakesSkipPathAndProducesEqualIndependentCopy()
    {
        var strFeature = new StringFeature("s");
        var fs = new FeatureStruct();
        fs.AddValue(strFeature, new StringFeatureValue("abc"));
        fs.Freeze();

        Assert.That(fs.IsTreeForTesting, Is.True);

        FeatureStruct clone = fs.Clone();

        Assert.That(clone, Is.Not.SameAs(fs));
        Assert.That(clone.IsFrozen, Is.False);
        Assert.That(clone.ValueEquals(fs), Is.True);

        // The clone is independently mutable and does not affect the frozen source.
        clone.AddValue(new StringFeature("t"), new StringFeatureValue("xyz"));
        Assert.That(clone.ValueEquals(fs), Is.False);
        Assert.That(fs.ContainsFeature(new StringFeature("t")), Is.False);
    }

    [Test]
    public void Clone_SharedLeafInstance_PreservesSharingInTheClone()
    {
        var strFeature = new StringFeature("s");
        var otherFeature = new StringFeature("t");
        var sharedLeaf = new StringFeatureValue("abc");

        var fs = new FeatureStruct();
        fs.AddValue(strFeature, sharedLeaf);
        fs.AddValue(otherFeature, sharedLeaf);
        // Deliberately left unfrozen: this exercises the general (non-skip) clone path regardless of freeze
        // state, since only frozen _isTree sources take the skip path.

        FeatureStruct clone = fs.Clone();

        var clonedS = clone.GetValue<StringFeatureValue>(strFeature);
        var clonedT = clone.GetValue<StringFeatureValue>(otherFeature);

        // The sharing pattern must be preserved: the two features of the clone must point to the SAME cloned
        // leaf instance, just as they did in the source.
        Assert.That(clonedS, Is.SameAs(clonedT));
        Assert.That(clone.ValueEquals(fs), Is.True);
    }

    [Test]
    public void Clone_NestedStructWithReentrance_PreservesSharingInTheClone()
    {
        var featSys = new FeatureSystem
        {
            new ComplexFeature("cx1"),
            new ComplexFeature("cx2"),
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2")),
        };

        FeatureStruct fs = FeatureStruct
            .New(featSys)
            .Feature("cx1")
            .EqualTo(1, cx1 => cx1.Symbol("a1"))
            .Feature("cx2")
            .ReferringTo(1)
            .Value;

        FeatureStruct clone = fs.Clone();

        var cx1 = clone.GetValue<FeatureStruct>((ComplexFeature)featSys.GetFeature("cx1"));
        var cx2 = clone.GetValue<FeatureStruct>((ComplexFeature)featSys.GetFeature("cx2"));

        Assert.That(cx1, Is.SameAs(cx2));
        Assert.That(clone.ValueEquals(fs), Is.True);
    }

    [Test]
    public void Clone_SelfReferentialCycle_TerminatesAndProducesEqualCopy()
    {
        var selfFeature = new ComplexFeature("self");
        var fs = new FeatureStruct();
        fs.AddValue(selfFeature, fs);

        FeatureStruct clone = fs;
        Assert.DoesNotThrow(() => clone = fs.Clone());

        var cloneSelf = clone.GetValue<FeatureStruct>(selfFeature);
        Assert.That(cloneSelf, Is.SameAs(clone));
        Assert.That(clone.ValueEquals(fs), Is.True);
    }

    // A StringFeatureValue whose CloneImpl() override makes a nested, unrelated top-level FeatureStruct.Clone()
    // call before delegating to the normal clone. Since SimpleFeatureValue.CloneImpl(copies) dispatches to this
    // override (via Clone()) from inside the outer FeatureStruct's clone loop, the nested call is forced to run
    // while the outer call still holds the copies pool.
    private sealed class ReentrantCloneProbeValue(string value, FeatureStruct nestedSource) : StringFeatureValue(value)
    {
        public bool NestedCallMade { get; private set; }
        public FeatureStruct? NestedClone { get; private set; }

        protected override SimpleFeatureValue CloneImpl()
        {
            NestedCallMade = true;
            NestedClone = nestedSource.Clone();
            return new ReentrantCloneProbeValue(Values.First(), nestedSource);
        }
    }

    [Test]
    public void Clone_ReentrantTopLevelCall_FallsBackAndIsCorrect()
    {
        var probeFeature = new StringFeature("probe");
        var leafFeature = new StringFeature("s");

        var nestedSource = new FeatureStruct();
        nestedSource.AddValue(leafFeature, new StringFeatureValue("nested"));

        var probe = new ReentrantCloneProbeValue("v", nestedSource);

        // The shared leaf instance below forces `fsOuter` onto the general (non-skip, pooled-copies) clone
        // path, so the probe's CloneImpl() override runs while the outer Clone() call holds the copies pool.
        var sharedLeaf = new StringFeatureValue("shared");
        var fsOuter = new FeatureStruct();
        fsOuter.AddValue(probeFeature, probe);
        fsOuter.AddValue(new StringFeature("shared1"), sharedLeaf);
        fsOuter.AddValue(new StringFeature("shared2"), sharedLeaf);

        FeatureStruct clone = fsOuter.Clone();

        Assert.That(probe.NestedCallMade, Is.True);
        Assert.That(
            probe.NestedClone!.ValueEquals(nestedSource),
            Is.True,
            "the nested top-level Clone() call, forced onto the fallback path, must still produce a correct copy"
        );
        Assert.That(probe.NestedClone, Is.Not.SameAs(nestedSource));

        // The outer clone itself must still be correct: sharing preserved, probe value cloned.
        Assert.That(clone.ValueEquals(fsOuter), Is.True);
    }
}
