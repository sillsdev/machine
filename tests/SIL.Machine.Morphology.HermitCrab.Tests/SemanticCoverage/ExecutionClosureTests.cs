using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ExecutionClosureTests
{
    private const string Dtd = "<!ELEMENT Root EMPTY>";

    private static SemanticInventory Inventory(string source, params string[] roots) =>
        SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[] { new CSharpInventoryInput("fixture.cs", source, roots) }
            )
        );

    [Test]
    public void ExactMethodRootFollowsDirectConstructorAccessorAndLocalCallsButExcludesDeadBodies()
    {
        const string Source = """
            using System.Xml.Linq;
            using SIL.Machine.Morphology.HermitCrab;
            namespace Fixture;
            public sealed class Root
            {
                public void Run(bool value)
                {
                    Helper(value);
                    _ = new Built(value);
                    _ = Value;
                    void Local() { if (value) { } }
                    Local();
                }

                private static void Helper(bool value)
                {
                    if (value) { }
                    _ = new XElement("root").Element("reachable");
                    SemanticBranch.Hit("reachable-marker");
                }

                private int Value { get { if (true) { } return 1; } }
                private void Dead(bool value)
                {
                    if (value) { }
                    _ = new XElement("root").Element("dead");
                    SemanticBranch.Hit("dead-marker");
                }
            }
            public sealed class Built
            {
                public Built(bool value) { if (value) { } }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(System.Boolean)");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind.StartsWith("decision-", StringComparison.Ordinal))
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(parent => parent, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parents, Does.Contain("Fixture.Root.Helper(System.Boolean)"));
            Assert.That(parents, Does.Contain("Fixture.Built..ctor(System.Boolean)"));
            Assert.That(parents, Does.Contain("Fixture.Root.Value/get"));
            Assert.That(parents, Has.Some.Contains("/local/Local()"));
            Assert.That(parents, Does.Not.Contain("Fixture.Root.Dead(System.Boolean)"));
            Assert.That(inventory.Surfaces.Select(surface => surface.Id), Does.Contain("branch:reachable-marker"));
            Assert.That(inventory.Surfaces.Select(surface => surface.Id), Does.Not.Contain("branch:dead-marker"));
            Assert.That(
                inventory.Surfaces.Where(surface => surface.Kind == "xml-read").Select(surface => surface.Name),
                Does.Contain("reachable")
            );
            Assert.That(
                inventory.Surfaces.Where(surface => surface.Kind == "xml-read").Select(surface => surface.Name),
                Does.Not.Contain("dead")
            );
        });
    }

    [Test]
    public void FiniteSourceInterfaceDispatchExpandsEveryImplementationDeterministically()
    {
        const string Source = """
            namespace Fixture;
            public interface IWorker { void Work(bool value); }
            public sealed class First : IWorker { public void Work(bool value) { if (value) { } } }
            public sealed class Second : IWorker { public void Work(bool value) { if (!value) { } } }
            public sealed class Root
            {
                public void Run(IWorker worker, bool value) { worker.Work(value); }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(Fixture.IWorker,System.Boolean)");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind == "decision-if")
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(parent => parent, StringComparer.Ordinal)
            .ToArray();

        Assert.That(
            parents,
            Is.EqualTo(new[] { "Fixture.First.Work(System.Boolean)", "Fixture.Second.Work(System.Boolean)" })
        );
        Assert.That(inventory.Diagnostics, Is.Empty);
    }

    [Test]
    public void RecursiveStronglyConnectedComponentTerminatesAndIncludesEveryReachableBodyOnce()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Root
            {
                public void First(bool value) { if (value) Second(value); }
                private void Second(bool value) { if (value) First(value); }
                private void Dead(bool value) { if (value) { } }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.First(System.Boolean)");

        Assert.That(
            inventory
                .Surfaces.Where(surface => surface.Kind == "decision-if")
                .Select(surface => surface.Parent)
                .Distinct(StringComparer.Ordinal),
            Is.EquivalentTo(new[] { "Fixture.Root.First(System.Boolean)", "Fixture.Root.Second(System.Boolean)" })
        );
    }

    [Test]
    public void MutableDelegateDispatchIsRetainedAsRedInventoryDiagnostic()
    {
        const string Source = """
            using System;
            namespace Fixture;
            public sealed class Root
            {
                public Func<bool, bool> Selector { get; set; } = value => value;
                public bool Run(bool value) => Selector(value);
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(System.Boolean)");

        Assert.That(
            inventory.Diagnostics.Select(diagnostic => diagnostic.Code),
            Does.Contain("unresolved-delegate-dispatch")
        );
        Assert.That(
            inventory.Diagnostics.Single(diagnostic => diagnostic.Code == "unresolved-delegate-dispatch").SubjectId,
            Is.EqualTo("Fixture.Root.Run(System.Boolean)")
        );
    }

    [Test]
    public void DynamicAndOpenExternalDispatchAreRetainedAsCallsiteSpecificDiagnostics()
    {
        const string Source = """
            using System;
            using System.IO;
            namespace Fixture;
            public sealed class Root
            {
                public int Run(dynamic target, Stream stream, byte[] buffer)
                {
                    target.Execute();
                    return stream.Read(buffer, 0, Math.Abs(buffer.Length));
                }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(dynamic,System.IO.Stream,System.Byte[])");

        Assert.Multiple(() =>
        {
            Assert.That(inventory.Diagnostics.Select(item => item.Code), Does.Contain("unresolved-call-dispatch"));
            Assert.That(inventory.Diagnostics.Select(item => item.Code), Does.Contain("external-virtual-dispatch"));
            Assert.That(
                inventory.Diagnostics,
                Has.None.Matches<InventoryDiagnostic>(item =>
                    item.Message.Contains("System.Math.Abs", StringComparison.Ordinal)
                )
            );
            Assert.That(inventory.Diagnostics.All(item => item.Location.Length != 0), Is.True);
            Assert.That(
                inventory.Diagnostics.Select(item => item.Location).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(inventory.Diagnostics.Count)
            );
        });
    }

    [Test]
    public void InterfaceDispatchWithoutConcreteSourceImplementationFailsClosed()
    {
        const string Source = """
            namespace Fixture;
            public interface IWorker { void Work(); }
            public abstract class Worker : IWorker { public abstract void Work(); }
            public sealed class Root { public void Run(IWorker worker) { worker.Work(); } }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(Fixture.IWorker)");

        Assert.That(inventory.Diagnostics.Select(item => item.Code), Does.Contain("unresolved-interface-dispatch"));
    }

    [Test]
    public void SourceBoundLambdaAndMethodGroupBodiesAreFollowedButUnusedClosuresAreExcluded()
    {
        const string Source = """
            using System;
            namespace Fixture;
            public sealed class Root
            {
                public bool Run(bool value)
                {
                    Func<bool, bool> lambda = flag => { if (flag) { } return flag; };
                    Func<bool, bool> method = Helper;
                    Func<bool, bool> unused = flag => { if (!flag) { } return flag; };
                    void UnusedLocal() { if (value) { } }
                    return lambda(value) && method(value);
                }
                private static bool Helper(bool value) { if (value) { } return value; }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(System.Boolean)");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind == "decision-if")
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parents, Does.Contain("Fixture.Root.Helper(System.Boolean)"));
            Assert.That(parents, Has.Some.Contains("lambda"));
            Assert.That(parents, Has.None.Contains("UnusedLocal"));
            Assert.That(parents.Count(parent => parent.Contains("lambda", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void StaticCallbackPassedThroughExternalApiIsFollowedAndMutableCallbackFailsClosed()
    {
        const string Source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            namespace Fixture;
            public sealed class Root
            {
                public Func<int, bool> Selector { get; set; } = value => true;
                public int Run(IEnumerable<int> values)
                {
                    _ = values.Where(IsPositive).ToArray();
                    return values.Where(Selector).Count();
                }
                private static bool IsPositive(int value) { if (value > 0) { } return value > 0; }
            }
            """;

        SemanticInventory inventory = Inventory(
            Source,
            "Fixture.Root.Run(System.Collections.Generic.IEnumerable`1<System.Int32>)"
        );

        Assert.Multiple(() =>
        {
            Assert.That(
                inventory.Surfaces.Where(surface => surface.Kind == "decision-if").Select(surface => surface.Parent),
                Does.Contain("Fixture.Root.IsPositive(System.Int32)")
            );
            Assert.That(inventory.Diagnostics.Select(item => item.Code), Does.Contain("unresolved-delegate-dispatch"));
        });
    }

    [Test]
    public void TypeReferencesDoNotExpandDeadMembersAndDeadUnresolvedMarkersAreIgnored()
    {
        const string Source = """
            using System;
            namespace Fixture;
            public sealed class Root
            {
                public Type Run() => typeof(Dead);
                private void DeadMarker(string value) { Missing.SemanticBranch.Hit(value); }
            }
            public sealed class Dead { public void Body(bool value) { if (value) { } } }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run()");

        Assert.Multiple(() =>
        {
            Assert.That(
                inventory.Surfaces.Where(surface => surface.Kind == "decision-if").Select(surface => surface.Parent),
                Does.Not.Contain("Fixture.Dead.Body(System.Boolean)")
            );
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void InterfacePropertyDispatchFollowsConcreteGettersWithoutIncludingSetters()
    {
        const string Source = """
            namespace Fixture;
            public interface IWorker { bool Value { get; } }
            public sealed class First : IWorker { public bool Value { get { if (true) { } return true; } } }
            public sealed class Second : IWorker { public bool Value { get { if (false) { } return false; } } }
            public sealed class Root
            {
                public bool Local { get { return true; } set { if (value) { } } }
                public bool Run(IWorker worker) => worker.Value && Local;
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(Fixture.IWorker)");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind == "decision-if")
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parents, Does.Contain("Fixture.First.Value/get"));
            Assert.That(parents, Does.Contain("Fixture.Second.Value/get"));
            Assert.That(parents, Does.Not.Contain("Fixture.Root.Local/set"));
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void SeparateUnresolvedDelegateCallsitesRemainSeparateDiagnostics()
    {
        const string Source = """
            using System;
            namespace Fixture;
            public sealed class Root
            {
                public Func<bool, bool> Selector { get; set; } = value => value;
                public bool Run(bool value)
                {
                    bool first = Selector(value);
                    bool second = Selector(!value);
                    return first && second;
                }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(System.Boolean)");
        InventoryDiagnostic[] diagnostics = inventory
            .Diagnostics.Where(item => item.Code == "unresolved-delegate-dispatch")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Length.EqualTo(2));
            Assert.That(
                diagnostics.Select(item => item.Location).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(2)
            );
        });
    }

    [Test]
    public void SemanticCompilationErrorsFailClosedBeforeClosure()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Root
            {
                public bool Run(string value)
                {
                    Missing.Execute();
                    return value.Equals("ok");
                }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(System.String)");

        Assert.That(inventory.Diagnostics.Select(item => item.Code), Does.Contain("compilation-error"));
    }

    [Test]
    public void ExactSealedMetadataDispatchIsNotAnOpenBoundary()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Root
            {
                public bool Run(string value) => value.Equals("ok");
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(System.String)");

        Assert.That(inventory.Diagnostics, Is.Empty);
    }

    [Test]
    public void ConstructionFollowsInstanceInitializersImplicitBaseConstructorAndExactBaseCall()
    {
        const string Source = """
            namespace Fixture;
            public class Base
            {
                public Base() { if (true) { } }
                public virtual bool Value(bool value) { if (value) { } return value; }
            }
            public sealed class Built : Base
            {
                private readonly bool _initialized = Initialize();
                public Built() { }
                public override bool Value(bool value) { if (!value) { } return base.Value(value); }
                public bool Run(bool value) => base.Value(value) && _initialized;
                private static bool Initialize() { if (true) { } return true; }
            }
            public sealed class Root { public Built Run() => new Built(); }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run()", "Fixture.Built.Run(System.Boolean)");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind == "decision-if")
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parents, Does.Contain("Fixture.Base..ctor()"));
            Assert.That(parents, Does.Contain("Fixture.Built.Initialize()"));
            Assert.That(parents, Does.Contain("Fixture.Base.Value(System.Boolean)"));
            Assert.That(parents, Does.Not.Contain("Fixture.Built.Value(System.Boolean)"));
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void OverrideChainsExpandPastAnIntermediateOverride()
    {
        const string Source = """
            namespace Fixture;
            public class Base { public virtual void Work(bool value) { if (value) { } } }
            public class Middle : Base { public override void Work(bool value) { if (!value) { } } }
            public sealed class Leaf : Middle { public override void Work(bool value) { if (value) { } } }
            public sealed class Root { public void Run(Middle worker, bool value) { worker.Work(value); } }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(Fixture.Middle,System.Boolean)");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind == "decision-if")
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parents, Does.Contain("Fixture.Middle.Work(System.Boolean)"));
            Assert.That(parents, Does.Contain("Fixture.Leaf.Work(System.Boolean)"));
            Assert.That(parents, Does.Not.Contain("Fixture.Base.Work(System.Boolean)"));
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ImplicitConstructionAndConstructorArgumentsExecuteInitializersAndBaseEdges()
    {
        const string Source = """
            namespace Fixture;
            public class Base
            {
                public Base(int value) { if (value > 0) { } }
            }
            public sealed class Built : Base
            {
                private readonly bool _value = Initialize();
                public Built() : base(Choose()) { }
                private static int Choose() { if (true) { } return 1; }
                private static bool Initialize() { if (true) { } return true; }
            }
            public sealed class Implicit
            {
                private readonly bool _value = Initialize();
                private readonly bool _direct = true ? true : false;
                private static bool Initialize() { if (true) { } return true; }
            }
            public sealed class Root
            {
                public void Run() { _ = new Built(); _ = new Implicit(); }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run()");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind == "decision-if")
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parents, Does.Contain("Fixture.Base..ctor(System.Int32)"));
            Assert.That(parents, Does.Contain("Fixture.Built.Choose()"));
            Assert.That(parents, Does.Contain("Fixture.Built.Initialize()"));
            Assert.That(parents, Does.Contain("Fixture.Implicit.Initialize()"));
            Assert.That(
                inventory
                    .Surfaces.Where(surface => surface.Kind == "decision-conditional")
                    .Select(surface => surface.Parent),
                Does.Contain("Fixture.Implicit._direct")
            );
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void CompoundAndBasePropertyAccessUseEveryExactAccessor()
    {
        const string Source = """
            namespace Fixture;
            public class Base
            {
                private int _value;
                public virtual int Value
                {
                    get { if (_value > 0) { } return _value; }
                    set { if (value > 0) { } _value = value; }
                }
            }
            public sealed class Derived : Base
            {
                public override int Value
                {
                    get { if (true) { } return 1; }
                    set { if (value < 0) { } }
                }
                public void Run() { base.Value += 1; }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Derived.Run()");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind == "decision-if")
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parents, Does.Contain("Fixture.Base.Value/get"));
            Assert.That(parents, Does.Contain("Fixture.Base.Value/set"));
            Assert.That(parents, Does.Not.Contain("Fixture.Derived.Value/get"));
            Assert.That(parents, Does.Not.Contain("Fixture.Derived.Value/set"));
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void OperatorsConversionsAndExpressionBodiedAccessorsAreExecutionEdges()
    {
        const string Source = """
            namespace Fixture;
            public readonly struct Number
            {
                public int Value => true ? 1 : 0;
                public static Number operator +(Number left, Number right)
                {
                    if (left.Value > 0) { }
                    return left;
                }
                public static explicit operator bool(Number value)
                {
                    if (value.Value > 0) { }
                    return true;
                }
            }
            public sealed class Root
            {
                public bool Run(Number left, Number right) => (bool)(left + right);
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(Fixture.Number,Fixture.Number)");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind.StartsWith("decision-", StringComparison.Ordinal))
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parents, Does.Contain("Fixture.Number.operator-op_Addition(Fixture.Number,Fixture.Number)"));
            Assert.That(parents, Does.Contain("Fixture.Number.conversion-op_Explicit(Fixture.Number)"));
            Assert.That(parents, Does.Contain("Fixture.Number.Value/get"));
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void DelegateFactoryExecutionIsFollowedBeforeTheReturnedDelegateIsDiagnosed()
    {
        const string Source = """
            using System;
            namespace Fixture;
            public sealed class Root
            {
                public bool Run(bool value)
                {
                    Func<bool> callback = Make(value);
                    return callback();
                }
                private static Func<bool> Make(bool value)
                {
                    if (value) { }
                    return () => value;
                }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(System.Boolean)");

        Assert.Multiple(() =>
        {
            Assert.That(
                inventory.Surfaces.Where(surface => surface.Kind == "decision-if").Select(surface => surface.Parent),
                Does.Contain("Fixture.Root.Make(System.Boolean)")
            );
            Assert.That(inventory.Diagnostics.Select(item => item.Code), Does.Contain("unresolved-delegate-dispatch"));
        });
    }

    [Test]
    public void CompileTimeDeadBranchesDoNotReachCallees()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Root
            {
                public void Run() { if (false) Dead(); }
                private static void Dead() { if (true) { } }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run()");

        Assert.That(
            inventory.Surfaces.Where(surface => surface.Kind == "decision-if").Select(surface => surface.Parent),
            Does.Not.Contain("Fixture.Root.Dead()")
        );
    }

    [Test]
    public void DiagnosticRecordPreservesFourValueDeconstructionCompatibility()
    {
        var diagnostic = new InventoryDiagnostic("code", "subject", "message", "configuration", "location");

        (string code, string subject, string message, string configurations) = diagnostic;

        Assert.That(
            new[] { code, subject, message, configurations },
            Is.EqualTo(new[] { "code", "subject", "message", "configuration" })
        );
        Assert.That(diagnostic.Location, Is.EqualTo("location"));
    }

    [Test]
    public void LambdaIdentityIsStableAcrossPreprocessorConfigurations()
    {
        const string Source = """
            using System;
            namespace Fixture;
            public sealed class Root
            {
                public bool Run(bool value)
                {
            #if OUTPUT_ANALYSES
                    Func<bool, bool> gated = flag => flag;
                    _ = gated(value);
            #endif
                    Func<bool, bool> shared = flag => { if (flag) { } return flag; };
                    return shared(value);
                }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(System.Boolean)");
        InventorySurface[] shared = inventory.Surfaces.Where(surface => surface.Kind == "decision-if").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                shared.Select(surface => surface.Parent).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(1)
            );
            Assert.That(
                shared.Select(surface => surface.Configurations).Distinct(StringComparer.Ordinal),
                Is.EqualTo(new[] { "OUTPUT_ANALYSES,OUTPUT_ANALYSES+SINGLE_THREADED,SINGLE_THREADED,base" })
            );
        });
    }

    [Test]
    public void StaticInitializationIsAnExecutionEdgeOfConstructionAndStaticUse()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Built
            {
                private static readonly bool Initialized = Initialize();
                static Built() { if (Initialized) { } }
                private static bool Initialize() { if (true) { } return true; }
                public static bool Value => Initialized;
            }
            public sealed class Root { public bool Run() { _ = new Built(); return Built.Value; } }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run()");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind == "decision-if")
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parents, Does.Contain("Fixture.Built..cctor()"));
            Assert.That(parents, Does.Contain("Fixture.Built.Initialize()"));
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void DynamicMemberAndGenericConstructionRemainExplicitOpenEdges()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Root
            {
                public object Read(dynamic value) { _ = value.Member; return value[0]; }
                public T Build<T>() where T : new() => new T();
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Read(dynamic)", "Fixture.Root.Build`1()");

        Assert.Multiple(() =>
        {
            Assert.That(inventory.Diagnostics.Count(item => item.Code == "unresolved-dynamic-member"), Is.EqualTo(2));
            Assert.That(inventory.Diagnostics.Select(item => item.Code), Does.Contain("open-construction-dispatch"));
            Assert.That(
                inventory.Diagnostics.Select(item => item.Location).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(inventory.Diagnostics.Count)
            );
        });
    }

    [Test]
    public void ConditionalAccessAndCoalescingAreInventoriedAsExecutionDecisions()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Item { public string Value => "value"; }
            public sealed class Root
            {
                public string Run(Item item, string fallback) => item?.Value ?? fallback;
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(Fixture.Item,System.String)");

        Assert.Multiple(() =>
        {
            Assert.That(
                inventory.Surfaces.Count(surface => surface.Kind == "decision-conditional-access"),
                Is.EqualTo(2)
            );
            Assert.That(inventory.Surfaces.Count(surface => surface.Kind == "decision-coalesce"), Is.EqualTo(2));
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void SourceDelegateParametersBindToConsumedCallbacksButIgnoreUnusedCallbacks()
    {
        const string Source = """
            using System;
            namespace Fixture;
            public sealed class Root
            {
                public bool Run(bool value)
                {
                    Consume(() => { if (value) { } return value; });
                    Ignore(() => { if (!value) { } return value; });
                    return value;
                }
                private static void Consume(Func<bool> callback) { if (true) { } _ = callback(); }
                private static void Ignore(Func<bool> callback) { }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(System.Boolean)");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind == "decision-if")
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parents, Does.Contain("Fixture.Root.Consume(System.Func`1<System.Boolean>)"));
            Assert.That(parents.Count(parent => parent.Contains("/lambda@", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void InheritedInterfaceImplementationMarksConcreteRuleTypeReachable()
    {
        const string Source = """
            using SIL.Machine.Annotations;
            using SIL.Machine.Morphology.HermitCrab;
            using SIL.Machine.Rules;
            namespace Fixture;
            public abstract class BaseRule : IHCRule
            {
                public string Name { get; set; }
                public IRule<Word, ShapeNode> CompileAnalysisRule(Morpher morpher)
                {
                    if (morpher is null) { }
                    return null;
                }
                public IRule<Word, ShapeNode> CompileSynthesisRule(Morpher morpher) => null;
            }
            public sealed class ConcreteRule : BaseRule { }
            public sealed class Root
            {
                public void Run(IHCRule rule, Morpher morpher) { _ = rule.CompileAnalysisRule(morpher); }
            }
            """;

        SemanticInventory inventory = Inventory(
            Source,
            "Fixture.Root.Run(SIL.Machine.Morphology.HermitCrab.IHCRule,SIL.Machine.Morphology.HermitCrab.Morpher)"
        );

        Assert.Multiple(() =>
        {
            Assert.That(
                inventory.Surfaces.Select(surface => surface.Id),
                Does.Contain("model:rule/Fixture.ConcreteRule/IHCRule")
            );
            Assert.That(
                inventory.Surfaces.Where(surface => surface.Kind == "decision-if").Select(surface => surface.Parent),
                Does.Contain("Fixture.BaseRule.CompileAnalysisRule(SIL.Machine.Morphology.HermitCrab.Morpher)")
            );
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void EverySemanticCompilationErrorFailsClosedBeforeClosure()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Root
            {
                public int Run()
                {
                    string invalid = 1;
                    return Helper();
                }
                private static int Helper() { if (true) { } return 1; }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root");

        Assert.Multiple(() =>
        {
            Assert.That(inventory.Diagnostics.Select(item => item.Code), Does.Contain("compilation-error"));
            Assert.That(
                inventory.Surfaces,
                Has.None.Matches<InventorySurface>(surface => surface.Parent == "Fixture.Root.Helper()")
            );
        });
    }

    [Test]
    public void RepositorySourcePathCannotBypassSemanticCompilationFailure()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Root
            {
                public int Run(string value)
                {
                    int invalid = value;
                    return invalid;
                }
            }
            """;
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[]
                {
                    new CSharpInventoryInput(
                        "src/Fixture/Root.cs",
                        Source,
                        new[] { "Fixture.Root.Run(System.String)" }
                    ),
                }
            )
        );

        Assert.That(inventory.Diagnostics.Select(item => item.Code), Does.Contain("compilation-error"));
    }

    [Test]
    public void ImplicitStaticInitializationWalksEveryInitializerFromStaticUse()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Built
            {
                private static readonly bool First = InitializeFirst();
                private static readonly bool Second = InitializeSecond();
                private static bool InitializeFirst() { if (true) { } return true; }
                private static bool InitializeSecond() { if (false) { } return false; }
                public static bool Value => First || Second;
            }
            public sealed class Root { public bool Run() => Built.Value; }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run()");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind == "decision-if")
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parents, Does.Contain("Fixture.Built.InitializeFirst()"));
            Assert.That(parents, Does.Contain("Fixture.Built.InitializeSecond()"));
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void InterfaceDispatchBindsCallbackToTheConsumingImplementation()
    {
        const string Source = """
            using System;
            namespace Fixture;
            public interface IConsumer { void Consume(Func<bool> callback); }
            public sealed class Consumer : IConsumer
            {
                public void Consume(Func<bool> callback) { _ = callback(); }
            }
            public sealed class Root
            {
                public bool Run(IConsumer consumer, bool value)
                {
                    consumer.Consume(() => { if (value) { } return value; });
                    return value;
                }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(Fixture.IConsumer,System.Boolean)");

        Assert.Multiple(() =>
        {
            Assert.That(
                inventory.Surfaces.Count(surface =>
                    surface.Kind == "decision-if" && surface.Parent!.Contains("/lambda@", StringComparison.Ordinal)
                ),
                Is.EqualTo(2)
            );
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void IgnoredDelegateStillEvaluatesItsFactoryWithoutInvokingTheReturnedCallback()
    {
        const string Source = """
            using System;
            namespace Fixture;
            public sealed class Root
            {
                public void Run() { Ignore(MakeCallback()); }
                private static void Ignore(Func<bool> callback) { }
                private static Func<bool> MakeCallback()
                {
                    if (true) { }
                    return () => { if (false) { } return false; };
                }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run()");
        string[] parents = inventory
            .Surfaces.Where(surface => surface.Kind == "decision-if")
            .Select(surface => surface.Parent!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parents, Does.Contain("Fixture.Root.MakeCallback()"));
            Assert.That(parents, Has.None.Contains("/lambda@"));
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void DelegateReferenceInsideDeadNestedFunctionDoesNotConsumeCallback()
    {
        const string Source = """
            using System;
            namespace Fixture;
            public sealed class Root
            {
                public void Run(bool value)
                {
                    Ignore(() => { if (value) { } return value; });
                }
                private static void Ignore(Func<bool> callback)
                {
                    void Dead() { _ = callback(); }
                }
            }
            """;

        SemanticInventory inventory = Inventory(Source, "Fixture.Root.Run(System.Boolean)");

        Assert.Multiple(() =>
        {
            Assert.That(
                inventory.Surfaces,
                Has.None.Matches<InventorySurface>(surface =>
                    surface.Parent?.Contains("/lambda@", StringComparison.Ordinal) == true
                )
            );
            Assert.That(inventory.Diagnostics, Is.Empty);
        });
    }
}
