using NUnit.Framework;

namespace SIL.Machine.FeatureModel;

// Covers the zero-allocation ValueEquals fast path for frozen, flat ("tree") feature structures: both sides
// frozen, and each individually proven at freeze time to contain no nested FeatureStruct and no repeated
// FeatureValue instance. Every test here cross-checks the fast path's answer against
// SlowValueEqualsForTesting, which forces the general visited-set-guarded comparison, so a divergence between
// the two would fail loudly.
[TestFixture]
public class FeatureStructTreeFastPathTests
{
    private static FeatureStruct CloneBySymbolicContent(FeatureStruct source)
    {
        var clone = new FeatureStruct();
        foreach (Feature feature in source.Features)
        {
            var value = source.GetValue<SymbolicFeatureValue>(feature);
            var symbol = (FeatureSymbol)value;
            clone.AddValue(feature, new SymbolicFeatureValue(symbol));
        }
        clone.Freeze();
        return clone;
    }

    private static SymbolicFeature CreateSymbolicFeature(string id, params string[] symbolIds)
    {
        var symbols = new FeatureSymbol[symbolIds.Length];
        for (int i = 0; i < symbolIds.Length; i++)
            symbols[i] = new FeatureSymbol(symbolIds[i]);
        return new SymbolicFeature(id, symbols);
    }

    [Test]
    public void ValueEquals_FrozenFlatStructs_EqualTakesFastPathAndIsCorrect()
    {
        SymbolicFeature a = CreateSymbolicFeature("a", "a1", "a2");
        SymbolicFeature b = CreateSymbolicFeature("b", "b1", "b2");

        var fs1 = new FeatureStruct();
        fs1.AddValue(a, new SymbolicFeatureValue((FeatureSymbol)a.PossibleSymbols["a1"]));
        fs1.AddValue(b, new SymbolicFeatureValue((FeatureSymbol)b.PossibleSymbols["b1"]));
        fs1.Freeze();

        var fs2 = new FeatureStruct();
        fs2.AddValue(a, new SymbolicFeatureValue((FeatureSymbol)a.PossibleSymbols["a1"]));
        fs2.AddValue(b, new SymbolicFeatureValue((FeatureSymbol)b.PossibleSymbols["b1"]));
        fs2.Freeze();

        Assert.That(fs1.IsTreeForTesting, Is.True);
        Assert.That(fs2.IsTreeForTesting, Is.True);

        bool result = fs1.ValueEquals(fs2);

        Assert.That(result, Is.True);
        Assert.That(result, Is.EqualTo(fs1.SlowValueEqualsForTesting(fs2)));
    }

    [Test]
    public void ValueEquals_FrozenFlatStructs_UnequalTakesFastPathAndIsCorrect()
    {
        SymbolicFeature a = CreateSymbolicFeature("a", "a1", "a2");

        var fs1 = new FeatureStruct();
        fs1.AddValue(a, new SymbolicFeatureValue((FeatureSymbol)a.PossibleSymbols["a1"]));
        fs1.Freeze();

        var fs2 = new FeatureStruct();
        fs2.AddValue(a, new SymbolicFeatureValue((FeatureSymbol)a.PossibleSymbols["a2"]));
        fs2.Freeze();

        // Different hash codes short-circuit before the fast-path check is even reached (same as before this
        // change), so this exercises that pre-existing short-circuit rather than the fast path itself; the
        // point of this test is that the answer is still correct.
        bool result = fs1.ValueEquals(fs2);

        Assert.That(result, Is.False);
        Assert.That(result, Is.EqualTo(fs1.SlowValueEqualsForTesting(fs2)));
    }

    [Test]
    public void ValueEquals_SharedLeafInstance_IsNotTreeAndStillComparesCorrectly()
    {
        var strFeature = new StringFeature("s");
        var otherFeature = new StringFeature("t");
        var sharedLeaf = new StringFeatureValue("abc");

        // The same leaf instance is referenced by two different features of fs1: not a tree.
        var fs1 = new FeatureStruct();
        fs1.AddValue(strFeature, sharedLeaf);
        fs1.AddValue(otherFeature, sharedLeaf);
        fs1.Freeze();

        var fs2 = new FeatureStruct();
        fs2.AddValue(strFeature, new StringFeatureValue("abc"));
        fs2.AddValue(otherFeature, new StringFeatureValue("abc"));
        fs2.Freeze();

        Assert.That(fs1.IsTreeForTesting, Is.False);

        bool result = fs1.ValueEquals(fs2);

        // The slow path treats leaf sharing the same way FeatureStructValueEqualsFreezeTests's reentrant-struct
        // tests treat sub-structure sharing: the sharing pattern itself is part of what is compared, so fs1
        // (one leaf instance shared by two features) is NOT equal to fs2 (two distinct, unshared instances with
        // equal content) even though every value matches. The point of this test is that the fast path -- which
        // requires _isTree, and therefore never runs here -- agrees with that slow-path answer rather than
        // silently changing it.
        Assert.That(result, Is.False);
        Assert.That(result, Is.EqualTo(fs1.SlowValueEqualsForTesting(fs2)));
    }

    [Test]
    public void ValueEquals_NestedStruct_IsNotTreeAndStillComparesCorrectly()
    {
        var complexFeature = new ComplexFeature("cx");
        var leafFeature = new StringFeature("s");

        var fs1 = new FeatureStruct();
        var nested1 = new FeatureStruct();
        nested1.AddValue(leafFeature, new StringFeatureValue("abc"));
        fs1.AddValue(complexFeature, nested1);
        fs1.Freeze();

        var fs2 = new FeatureStruct();
        var nested2 = new FeatureStruct();
        nested2.AddValue(leafFeature, new StringFeatureValue("abc"));
        fs2.AddValue(complexFeature, nested2);
        fs2.Freeze();

        Assert.That(fs1.IsTreeForTesting, Is.False);
        Assert.That(nested1.IsTreeForTesting, Is.True, "the nested struct itself is flat, only its parent is not");

        bool result = fs1.ValueEquals(fs2);

        Assert.That(result, Is.True);
        Assert.That(result, Is.EqualTo(fs1.SlowValueEqualsForTesting(fs2)));
    }

    [Test]
    public void ValueEquals_RandomFlatStructs_FastPathAgreesWithSlowPath()
    {
        SymbolicFeature[] features =
        {
            CreateSymbolicFeature("f0", "s0", "s1", "s2", "s3"),
            CreateSymbolicFeature("f1", "s0", "s1", "s2", "s3"),
            CreateSymbolicFeature("f2", "s0", "s1", "s2", "s3"),
            CreateSymbolicFeature("f3", "s0", "s1", "s2", "s3"),
        };

        var random = new Random(12345);
        FeatureStruct MakeRandomFlatStruct()
        {
            var fs = new FeatureStruct();
            int featureCount = random.Next(1, 5); // 1-4 features
            // Choose a random subset (without repeats) of the 4 features.
            var indices = new System.Collections.Generic.List<int> { 0, 1, 2, 3 };
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            for (int i = 0; i < featureCount; i++)
            {
                SymbolicFeature feature = features[indices[i]];
                var symbols = new System.Collections.Generic.List<FeatureSymbol>();
                foreach (FeatureSymbol symbol in feature.PossibleSymbols)
                    symbols.Add(symbol);
                FeatureSymbol chosen = symbols[random.Next(symbols.Count)];
                fs.AddValue(feature, new SymbolicFeatureValue(chosen));
            }
            fs.Freeze();
            return fs;
        }

        const int StructCount = 200;
        var structs = new FeatureStruct[StructCount];
        for (int i = 0; i < StructCount; i++)
        {
            structs[i] = MakeRandomFlatStruct();
            Assert.That(structs[i].IsTreeForTesting, Is.True);
        }

        // Every neighbor pair, whatever its hash relationship, must agree between the fast path (taken whenever
        // both sides are frozen _isTree structs and the pre-existing hash short-circuit doesn't already answer
        // the question) and the forced slow path.
        int comparisons = 0;
        for (int i = 0; i < StructCount; i++)
        {
            FeatureStruct other = structs[(i + 1) % StructCount];
            bool fast = structs[i].ValueEquals(other);
            bool slow = structs[i].SlowValueEqualsForTesting(other);
            Assert.That(fast, Is.EqualTo(slow), $"mismatch comparing struct {i} to its neighbor");
            comparisons++;
        }
        Assert.That(comparisons, Is.EqualTo(StructCount));

        // Also compare every struct against an exact duplicate of itself (same feature/symbol content, built
        // independently so it is a distinct, equally-frozen _isTree instance) -- these are guaranteed to have
        // matching hash codes, so they are guaranteed to reach and exercise the fast path itself, not just the
        // pre-existing hash short-circuit.
        for (int i = 0; i < StructCount; i++)
        {
            FeatureStruct duplicate = CloneBySymbolicContent(structs[i]);
            bool fast = structs[i].ValueEquals(duplicate);
            bool slow = structs[i].SlowValueEqualsForTesting(duplicate);
            Assert.That(fast, Is.True, $"struct {i} should equal its own duplicate");
            Assert.That(fast, Is.EqualTo(slow), $"mismatch comparing struct {i} to its duplicate");
        }
    }
}
