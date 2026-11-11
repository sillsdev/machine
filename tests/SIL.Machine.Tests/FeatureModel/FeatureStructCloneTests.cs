using NUnit.Framework;

namespace SIL.Machine.FeatureModel;

[TestFixture]
public class FeatureStructCloneTests
{
    // Exposes the protected map-based copy constructor, the reference behaviour Clone() must match.
    private class MapCopy(FeatureStruct other) : FeatureStruct(other) { }

    private static FeatureSystem CreateFeatureSystem()
    {
        return new FeatureSystem
        {
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2")),
            new SymbolicFeature("b", new FeatureSymbol("b1"), new FeatureSymbol("b2")),
            new ComplexFeature("cx1"),
            new ComplexFeature("cx2"),
            new ComplexFeature("cx3"),
        };
    }

    private static void AssertMatchesMapCopy(FeatureStruct fs)
    {
        FeatureStruct clone = fs.Clone();
        FeatureStruct mapCopy = new MapCopy(fs);
        Assert.That(clone, Is.Not.SameAs(fs));
        Assert.That(clone.ValueEquals(mapCopy), Is.True);
        Assert.That(mapCopy.ValueEquals(clone), Is.True);
        Assert.That(clone.ValueEquals(fs), Is.True);
        Assert.That(clone.ToString(), Is.EqualTo(mapCopy.ToString()));
    }

    [Test]
    public void Clone_PlainTree_MatchesMapCopyAndIsIndependent()
    {
        FeatureSystem featSys = CreateFeatureSystem();
        FeatureStruct fs = FeatureStruct
            .NewMutable(featSys)
            .Symbol("a1", "a2")
            .Feature("cx1")
            .EqualTo(cx1 => cx1.Symbol("b1"))
            .Feature("cx2")
            .EqualTo(cx2 => cx2.Symbol("b2").Feature("cx3").EqualTo(cx3 => cx3.Symbol("a1")))
            .Value;
        AssertMatchesMapCopy(fs);

        FeatureStruct clone = fs.Clone();
        Assert.That(clone.GetValue<FeatureStruct>("cx1"), Is.Not.SameAs(fs.GetValue<FeatureStruct>("cx1")));
        clone.GetValue<FeatureStruct>("cx1").AddValue(featSys.GetFeature<SymbolicFeature>("a"), featSys.GetSymbol("a2"));
        Assert.That(clone.ValueEquals(fs), Is.False);
    }

    [Test]
    public void Clone_Reentrant_PreservesSharing()
    {
        FeatureSystem featSys = CreateFeatureSystem();
        FeatureStruct fs = FeatureStruct
            .NewMutable(featSys)
            .Feature("cx1")
            .EqualTo(1, cx1 => cx1.Symbol("a1"))
            .Feature("cx2")
            .ReferringTo(1)
            .Value;
        Assert.That(fs.GetValue<FeatureStruct>("cx1"), Is.SameAs(fs.GetValue<FeatureStruct>("cx2")));
        AssertMatchesMapCopy(fs);

        FeatureStruct clone = fs.Clone();
        FeatureStruct shared = clone.GetValue<FeatureStruct>("cx1");
        Assert.That(shared, Is.SameAs(clone.GetValue<FeatureStruct>("cx2")));
        Assert.That(shared, Is.Not.SameAs(fs.GetValue<FeatureStruct>("cx1")));
    }

    [Test]
    public void Clone_ForwardChain_FollowsForwardAndPreservesSharing()
    {
        FeatureSystem featSys = CreateFeatureSystem();
        var target = new FeatureStruct();
        target.AddValue(featSys.GetFeature<SymbolicFeature>("a"), featSys.GetSymbol("a1"));
        var middle = new FeatureStruct();
        middle.AddValue(featSys.GetFeature<SymbolicFeature>("b"), featSys.GetSymbol("b1"));
        var head = new FeatureStruct();
        head.AddValue(featSys.GetFeature<SymbolicFeature>("b"), featSys.GetSymbol("b2"));
        head.Forward = middle;
        middle.Forward = target;

        // two features reach the same structure through different points of the Forward chain
        var fs = new FeatureStruct();
        fs.AddValue(featSys.GetFeature<ComplexFeature>("cx1"), head);
        fs.AddValue(featSys.GetFeature<ComplexFeature>("cx2"), target);
        AssertMatchesMapCopy(fs);
        FeatureStruct clone = fs.Clone();
        Assert.That(clone.GetValue<FeatureStruct>("cx1"), Is.SameAs(clone.GetValue<FeatureStruct>("cx2")));
        Assert.That(clone.GetValue<FeatureStruct>("cx1"), Is.Not.SameAs(target));

        // a Forward chain used once is simply followed
        var single = new FeatureStruct();
        single.AddValue(featSys.GetFeature<ComplexFeature>("cx1"), head);
        AssertMatchesMapCopy(single);
        Assert.That(single.Clone().GetValue<FeatureStruct>("cx1").ValueEquals(target), Is.True);
    }

    [Test]
    public void Clone_Cycle_MatchesMapCopy()
    {
        FeatureSystem featSys = CreateFeatureSystem();
        var fs = new FeatureStruct();
        fs.AddValue(featSys.GetFeature<SymbolicFeature>("a"), featSys.GetSymbol("a1"));
        fs.AddValue(featSys.GetFeature<ComplexFeature>("cx1"), fs);

        FeatureStruct clone = fs.Clone();
        FeatureStruct mapCopy = new MapCopy(fs);
        Assert.That(clone, Is.Not.SameAs(fs));
        Assert.That(clone.GetValue<FeatureStruct>("cx1"), Is.SameAs(clone));
        Assert.That(mapCopy.GetValue<FeatureStruct>("cx1"), Is.SameAs(mapCopy));
        Assert.That(clone.ValueEquals(mapCopy), Is.True);
    }

    [TestCase(10)]
    [TestCase(16)]
    [TestCase(17)]
    [TestCase(40)]
    public void Clone_NestingAroundTheNodeLimit_MatchesMapCopy(int depth)
    {
        // each level holds a symbolic value and a child, so the node count is about 2 * depth (limit is 32)
        FeatureSystem featSys = CreateFeatureSystem();
        var root = new FeatureStruct();
        FeatureStruct current = root;
        for (int i = 0; i < depth; i++)
        {
            current.AddValue(featSys.GetFeature<SymbolicFeature>("a"), featSys.GetSymbol(i % 2 == 0 ? "a1" : "a2"));
            var child = new FeatureStruct();
            current.AddValue(featSys.GetFeature<ComplexFeature>("cx1"), child);
            current = child;
        }
        AssertMatchesMapCopy(root);
    }

    [Test]
    public void Clone_LargeReentrantStructure_PreservesSharing()
    {
        FeatureSystem featSys = CreateFeatureSystem();
        var shared = new FeatureStruct();
        shared.AddValue(featSys.GetFeature<SymbolicFeature>("b"), featSys.GetSymbol("b1"));
        var root = new FeatureStruct();
        FeatureStruct current = root;
        for (int i = 0; i < 20; i++)
        {
            current.AddValue(featSys.GetFeature<SymbolicFeature>("a"), featSys.GetSymbol("a1"));
            current.AddValue(featSys.GetFeature<ComplexFeature>("cx2"), shared);
            var child = new FeatureStruct();
            current.AddValue(featSys.GetFeature<ComplexFeature>("cx1"), child);
            current = child;
        }
        AssertMatchesMapCopy(root);
        FeatureStruct clone = root.Clone();
        Assert.That(
            clone.GetValue<FeatureStruct>("cx2"),
            Is.SameAs(clone.GetValue<FeatureStruct>("cx1").GetValue<FeatureStruct>("cx2"))
        );
        Assert.That(clone.GetValue<FeatureStruct>("cx2"), Is.Not.SameAs(shared));
    }

    private static void AssertBindings(VariableBindings actual, params (string, string)[] expected)
    {
        Assert.That(actual.Count, Is.EqualTo(expected.Length));
        foreach ((string name, string symbol) in expected)
            Assert.That(actual[name].ToString(), Is.EqualTo(symbol));
    }

    private static (FeatureStruct, FeatureStruct, VariableBindings) CreateFailingAfterBindingCase()
    {
        // var1 is bound while processing "a", then "b" conflicts, so the whole operation fails
        var featSys = new FeatureSystem
        {
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2")),
            new SymbolicFeature("b", new FeatureSymbol("b1"), new FeatureSymbol("b2")),
            new SymbolicFeature("c", new FeatureSymbol("c1"), new FeatureSymbol("c2")),
        };
        FeatureStruct fs1 = FeatureStruct.NewMutable(featSys).Symbol("a1").Symbol("b1").Symbol("c1").Value;
        FeatureStruct fs2 = FeatureStruct
            .NewMutable(featSys)
            .Feature("a")
            .EqualToVariable("var1")
            .Feature("c")
            .EqualToVariable("var2")
            .Symbol("b2")
            .Value;
        var bindings = new VariableBindings();
        bindings["existing"] = new SymbolicFeatureValue(featSys.GetSymbol("c2"));
        return (fs1, fs2, bindings);
    }

    [Test]
    public void VariableBindings_FailedUnify_LeavesBindingsUnchanged()
    {
        (FeatureStruct fs1, FeatureStruct fs2, VariableBindings bindings) = CreateFailingAfterBindingCase();
        Assert.That(fs1.Unify(fs2, bindings, out _), Is.False);
        AssertBindings(bindings, ("existing", "c2"));
    }

    [Test]
    public void VariableBindings_FailedIsUnifiable_LeavesBindingsUnchanged()
    {
        (FeatureStruct fs1, FeatureStruct fs2, VariableBindings bindings) = CreateFailingAfterBindingCase();
        Assert.That(fs1.IsUnifiable(fs2, bindings), Is.False);
        AssertBindings(bindings, ("existing", "c2"));
    }

    [Test]
    public void VariableBindings_FailedSubsumes_LeavesBindingsUnchanged()
    {
        (FeatureStruct fs1, FeatureStruct fs2, VariableBindings bindings) = CreateFailingAfterBindingCase();
        // fs2 (with variables) is the more general side
        Assert.That(fs2.Subsumes(fs1, bindings), Is.False);
        AssertBindings(bindings, ("existing", "c2"));
    }

    [Test]
    public void VariableBindings_SuccessfulOperations_KeepNewBindings()
    {
        var featSys = new FeatureSystem
        {
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2")),
            new SymbolicFeature("b", new FeatureSymbol("b1"), new FeatureSymbol("b2")),
        };
        FeatureStruct fs1 = FeatureStruct.NewMutable(featSys).Symbol("a1").Symbol("b1").Value;
        FeatureStruct fs2 = FeatureStruct.NewMutable(featSys).Feature("a").EqualToVariable("var1").Symbol("b1").Value;

        var bindings = new VariableBindings();
        Assert.That(fs1.Unify(fs2, bindings, out _), Is.True);
        Assert.That(bindings.Count, Is.EqualTo(1));

        bindings = new VariableBindings();
        Assert.That(fs1.IsUnifiable(fs2, bindings), Is.True);
        Assert.That(bindings.Count, Is.EqualTo(1));

        bindings = new VariableBindings();
        Assert.That(fs2.Subsumes(fs1, bindings), Is.True);
        Assert.That(bindings.Count, Is.EqualTo(1));
    }
}
