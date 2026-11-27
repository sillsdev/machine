using NUnit.Framework;

namespace SIL.Machine.FeatureModel;

[TestFixture]
public class VariableBindingsTests
{
    private static (FeatureStruct, FeatureStruct) CreateConflicting(out FeatureSystem featSys)
    {
        featSys = new FeatureSystem
        {
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2")),
            new SymbolicFeature("b", new FeatureSymbol("b1"), new FeatureSymbol("b2")),
        };
        FeatureStruct fs1 = FeatureStruct.New(featSys).Symbol("a1").Feature("b").EqualToVariable("x").Value;
        FeatureStruct fs2 = FeatureStruct.New(featSys).Symbol("a2").Feature("b").EqualToVariable("x").Value;
        return (fs1, fs2);
    }

    [Test]
    public void ConcurrentUse_Throws()
    {
        (FeatureStruct fs1, FeatureStruct fs2) = CreateConflicting(out _);
        var varBindings = new VariableBindings();
        Assert.That(varBindings.BeginTransaction(), Is.True);
        try
        {
            Assert.That(
                () => Task.Run(() => fs1.IsUnifiable(fs2, varBindings)).GetAwaiter().GetResult(),
                Throws.InvalidOperationException
            );
            Assert.That(
                () => Task.Run(() => fs1.Unify(fs2, varBindings, out _)).GetAwaiter().GetResult(),
                Throws.InvalidOperationException
            );
        }
        finally
        {
            varBindings.EndTransaction(false);
        }
        Assert.That(fs1.IsUnifiable(fs2, varBindings), Is.False);
    }

    [Test]
    public void SameThreadNesting_DoesNotThrow()
    {
        var varBindings = new VariableBindings();
        Assert.That(varBindings.BeginTransaction(), Is.True);
        Assert.That(varBindings.BeginTransaction(), Is.False);
        varBindings.EndTransaction(true);
        Assert.That(varBindings.BeginTransaction(), Is.True);
        varBindings.EndTransaction(true);
    }

    [Test]
    public void FailedOperation_LeavesBindingsUnchanged()
    {
        (FeatureStruct fs1, FeatureStruct fs2) = CreateConflicting(out _);
        var varBindings = new VariableBindings();
        Assert.That(fs1.IsUnifiable(fs2, varBindings), Is.False);
        Assert.That(fs1.Unify(fs2, varBindings, out _), Is.False);
        Assert.That(fs1.Subsumes(fs2, varBindings), Is.False);
        Assert.That(varBindings.Count, Is.EqualTo(0));
    }
}
