using NUnit.Framework;

namespace SIL.Machine.FeatureModel;

// Covers ValueEquals/Freeze/GetFrozenHashCode over flat, nested, and reentrant/cyclic FeatureStruct graphs.
// These invariants must hold identically whether the visited sets used to guard the recursive walk are
// allocated eagerly or lazily.
[TestFixture]
public class FeatureStructValueEqualsFreezeTests
{
    private static FeatureSystem CreateFeatureSystem()
    {
        return new FeatureSystem
        {
            new StringFeature("s"),
            new SymbolicFeature("sym", new FeatureSymbol("x"), new FeatureSymbol("y")),
            new ComplexFeature("cx"),
        };
    }

    [Test]
    public void ValueEquals_FlatStruct_EqualReturnsTrue()
    {
        FeatureSystem featSys = CreateFeatureSystem();
        FeatureStruct fs1 = FeatureStruct.New(featSys).Symbol("x").Feature("s").EqualTo("abc").Value;
        FeatureStruct fs2 = FeatureStruct.New(featSys).Symbol("x").Feature("s").EqualTo("abc").Value;

        Assert.That(fs1.ValueEquals(fs2), Is.True);
        Assert.That(fs2.ValueEquals(fs1), Is.True);
    }

    [Test]
    public void ValueEquals_FlatStruct_UnequalReturnsFalse()
    {
        FeatureSystem featSys = CreateFeatureSystem();
        FeatureStruct fs1 = FeatureStruct.New(featSys).Symbol("x").Feature("s").EqualTo("abc").Value;
        FeatureStruct fs2 = FeatureStruct.New(featSys).Symbol("y").Feature("s").EqualTo("abc").Value;

        Assert.That(fs1.ValueEquals(fs2), Is.False);
    }

    [Test]
    public void GetFrozenHashCode_FlatStruct_EqualStructsHaveEqualHash()
    {
        FeatureSystem featSys = CreateFeatureSystem();
        FeatureStruct fs1 = FeatureStruct.New(featSys).Symbol("x").Feature("s").EqualTo("abc").Value;
        FeatureStruct fs2 = FeatureStruct.New(featSys).Symbol("x").Feature("s").EqualTo("abc").Value;
        fs1.Freeze();
        fs2.Freeze();

        Assert.That(fs1.GetFrozenHashCode(), Is.EqualTo(fs2.GetFrozenHashCode()));
        Assert.That(fs1.ValueEquals(fs2), Is.True);
    }

    [Test]
    public void ValueEquals_NestedStruct_EqualAndUnequal()
    {
        FeatureSystem featSys = CreateFeatureSystem();
        FeatureStruct fs1 = FeatureStruct.New(featSys).Feature("cx").EqualTo(cx => cx.Symbol("x")).Value;
        FeatureStruct fs2 = FeatureStruct.New(featSys).Feature("cx").EqualTo(cx => cx.Symbol("x")).Value;
        FeatureStruct fs3 = FeatureStruct.New(featSys).Feature("cx").EqualTo(cx => cx.Symbol("y")).Value;

        Assert.That(fs1.ValueEquals(fs2), Is.True);
        Assert.That(fs1.ValueEquals(fs3), Is.False);
    }

    [Test]
    public void ValueEquals_ReentrantStructure_MatchingReentranceIsEqual()
    {
        FeatureSystem featSys = new FeatureSystem
        {
            new ComplexFeature("cx1"),
            new ComplexFeature("cx2"),
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2")),
        };

        // cx1 and cx2 point to the exact same sub-structure instance in both fs1 and fs2.
        FeatureStruct fs1 = FeatureStruct
            .New(featSys)
            .Feature("cx1")
            .EqualTo(1, cx1 => cx1.Symbol("a1"))
            .Feature("cx2")
            .ReferringTo(1)
            .Value;

        FeatureStruct fs2 = FeatureStruct
            .New(featSys)
            .Feature("cx1")
            .EqualTo(1, cx1 => cx1.Symbol("a1"))
            .Feature("cx2")
            .ReferringTo(1)
            .Value;

        Assert.That(fs1.ValueEquals(fs2), Is.True);
    }

    [Test]
    public void ValueEquals_ReentrantStructure_NonReentrantCounterpartIsUnequal()
    {
        FeatureSystem featSys = new FeatureSystem
        {
            new ComplexFeature("cx1"),
            new ComplexFeature("cx2"),
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2")),
        };

        // fs1: cx1 and cx2 refer to the SAME sub-structure instance (reentrant).
        FeatureStruct fs1 = FeatureStruct
            .New(featSys)
            .Feature("cx1")
            .EqualTo(1, cx1 => cx1.Symbol("a1"))
            .Feature("cx2")
            .ReferringTo(1)
            .Value;

        // fs2: cx1 and cx2 have equal *values* but are two distinct, unshared sub-structure instances.
        FeatureStruct fs2 = FeatureStruct
            .New(featSys)
            .Feature("cx1")
            .EqualTo(cx1 => cx1.Symbol("a1"))
            .Feature("cx2")
            .EqualTo(cx2 => cx2.Symbol("a1"))
            .Value;

        // The reentrance pattern itself is part of what ValueEquals checks, so these must NOT be equal even
        // though every leaf value matches.
        Assert.That(fs1.ValueEquals(fs2), Is.False);
        Assert.That(fs2.ValueEquals(fs1), Is.False);
    }

    [Test]
    public void ValueEquals_SelfReferentialCycle_TerminatesAndIsEqual()
    {
        var selfFeature = new ComplexFeature("self");

        var fs1 = new FeatureStruct();
        fs1.AddValue(selfFeature, fs1);

        var fs2 = new FeatureStruct();
        fs2.AddValue(selfFeature, fs2);

        Assert.That(fs1.ValueEquals(fs2), Is.True);
    }

    [Test]
    public void ValueEquals_SelfReferentialCycle_DifferentShapeIsUnequal()
    {
        var selfFeature = new ComplexFeature("self");
        var otherFeature = new StringFeature("other");

        var fs1 = new FeatureStruct();
        fs1.AddValue(selfFeature, fs1);

        var fs2 = new FeatureStruct();
        fs2.AddValue(selfFeature, fs2);
        fs2.AddValue(otherFeature, "abc");

        Assert.That(fs1.ValueEquals(fs2), Is.False);
    }

    [Test]
    public void Freeze_SelfReferentialCycle_TerminatesAndHashIsStable()
    {
        var selfFeature = new ComplexFeature("self");

        var fs = new FeatureStruct();
        fs.AddValue(selfFeature, fs);

        Assert.DoesNotThrow(fs.Freeze);
        Assert.That(fs.IsFrozen, Is.True);

        int hash1 = fs.GetFrozenHashCode();
        int hash2 = fs.GetFrozenHashCode();
        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void Freeze_SharedChild_HashDoesNotDependOnWhetherChildWasPreFrozen()
    {
        var leftFeature = new ComplexFeature("left");
        var rightFeature = new ComplexFeature("right");
        var valueFeature = new StringFeature("value");

        var preFrozenChild = new FeatureStruct();
        preFrozenChild.AddValue(valueFeature, "same");
        preFrozenChild.Freeze();
        var parentWithPreFrozenChild = new FeatureStruct();
        parentWithPreFrozenChild.AddValue(leftFeature, preFrozenChild);
        parentWithPreFrozenChild.AddValue(rightFeature, preFrozenChild);

        var childFrozenDuringParentWalk = new FeatureStruct();
        childFrozenDuringParentWalk.AddValue(valueFeature, "same");
        var parentThatFreezesChild = new FeatureStruct();
        parentThatFreezesChild.AddValue(leftFeature, childFrozenDuringParentWalk);
        parentThatFreezesChild.AddValue(rightFeature, childFrozenDuringParentWalk);

        parentWithPreFrozenChild.Freeze();
        parentThatFreezesChild.Freeze();

        Assert.That(parentWithPreFrozenChild.ValueEquals(parentThatFreezesChild), Is.True);
        Assert.That(
            parentWithPreFrozenChild.GetFrozenHashCode(),
            Is.EqualTo(parentThatFreezesChild.GetFrozenHashCode())
        );
    }

    [Test]
    public void Freeze_CyclicChild_HashDoesNotDependOnWhichNodeWasPreFrozen()
    {
        var childFeature = new ComplexFeature("child");
        var parentFeature = new ComplexFeature("parent");
        var valueFeature = new StringFeature("value");

        var parentWithPreFrozenChild = new FeatureStruct();
        var preFrozenChild = new FeatureStruct();
        parentWithPreFrozenChild.AddValue(childFeature, preFrozenChild);
        preFrozenChild.AddValue(parentFeature, parentWithPreFrozenChild);
        preFrozenChild.AddValue(valueFeature, "same");

        var directlyFrozenParent = new FeatureStruct();
        var childFrozenDuringParentWalk = new FeatureStruct();
        directlyFrozenParent.AddValue(childFeature, childFrozenDuringParentWalk);
        childFrozenDuringParentWalk.AddValue(parentFeature, directlyFrozenParent);
        childFrozenDuringParentWalk.AddValue(valueFeature, "same");

        preFrozenChild.Freeze();
        Assert.That(parentWithPreFrozenChild.ValueEquals(directlyFrozenParent), Is.True);

        int parentWithPreFrozenChildHash = parentWithPreFrozenChild.GetFrozenHashCode();
        directlyFrozenParent.Freeze();

        Assert.That(parentWithPreFrozenChildHash, Is.EqualTo(directlyFrozenParent.GetFrozenHashCode()));
        Assert.That(parentWithPreFrozenChild.ValueEquals(directlyFrozenParent), Is.True);
    }
}
