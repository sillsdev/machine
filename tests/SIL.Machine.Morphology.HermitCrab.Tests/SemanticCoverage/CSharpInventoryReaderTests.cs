using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class CSharpInventoryReaderTests
{
    private const string Dtd = "<!ELEMENT Root EMPTY>";

    [Test]
    public void SemanticCompilationUsesCanonicalOverloadAndDistinguishesLiteralAndDynamicElementNames()
    {
        const string Source = """
            using System.Xml.Linq;
            using SIL.Machine.Morphology.HermitCrab;
            using SIL.Machine.Rules;
            using SIL.Machine.Annotations;
            using System.Collections.Generic;
            using System.Linq;
            namespace Fixture;
            public sealed class Foo { public string Element(string name) => name; }
            public sealed class Loader
            {
                public XElement Load(string name) => new XElement("root").Element(name);
                public XElement Load(int name) => new XElement("root").Element(nameof(name));
                public string Unrelated() => new Foo().Element("fake");
            }
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[]
                {
                    new CSharpInventoryInput(
                        "loader.cs",
                        Source,
                        new[] { "Fixture.Loader.Load(System.String)", "Fixture.Loader.Load(System.Int32)" }
                    ),
                }
            )
        );

        Assert.That(
            inventory.Surfaces.Select(surface => surface.Id),
            Does.Contain("source:dynamic-xml-access/Fixture.Loader.Load(System.String)/Element#0")
        );
        Assert.That(
            inventory.Surfaces.Select(surface => surface.Id),
            Does.Contain("loader:Fixture.Loader.Load(System.Int32)/Element/name#0")
        );
        var xmlParents = inventory
            .Surfaces.Where(surface => surface.Kind is "xml-read" or "dynamic-xml-access")
            .Select(surface => surface.Parent)
            .Where(parent => parent is not null)
            .ToArray();
        Assert.That(xmlParents, Does.Contain("Fixture.Loader.Load(System.String)"));
        Assert.That(xmlParents, Does.Contain("Fixture.Loader.Load(System.Int32)"));
    }

    [Test]
    public void XmlResolutionUsesSemanticSymbolsAndFailsClosedForUnrelatedApis()
    {
        const string Source = """
            using Xml = System.Xml.Linq;
            namespace Fixture;
            public sealed class Unrelated
            {
                public string Element(string name) => name;
                public string Elements() => string.Empty;
                public string Attribute(string name) => name;
            }
            public sealed class Loader
            {
                public void Read(Xml.XElement xml, string name)
                {
                    xml.Element("literal");
                    xml.Element(nameof(name));
                    xml.Element("pre" + "fix");
                    xml.Element(name);
                    xml.Elements();
                    xml.Elements("many");
                    xml?.Element("conditional");
                    new Unrelated().Element("fake");
                    new Unrelated().Elements();
                    new Unrelated().Attribute("fake");
                }
            }
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet("fixture.dtd", Dtd, new[] { new CSharpInventoryInput("fixture.cs", Source) })
        );
        var loaderSurfaces = inventory
            .Surfaces.Where(surface => surface.Parent == "Fixture.Loader.Read(System.Xml.Linq.XElement,System.String)")
            .ToArray();
        Assert.That(
            loaderSurfaces.Where(surface => surface.Kind == "xml-read").Select(surface => surface.Name),
            Is.EquivalentTo(new[] { "literal", "name", "prefix", "many", "conditional" })
        );
        Assert.That(loaderSurfaces.Count(surface => surface.Kind == "dynamic-xml-access"), Is.EqualTo(1));
        Assert.That(loaderSurfaces.Count(surface => surface.Kind == "xml-all-elements"), Is.EqualTo(1));
        Assert.That(
            loaderSurfaces.Any(surface =>
                surface.Parent?.StartsWith("Fixture.Unrelated", StringComparison.Ordinal) == true
            ),
            Is.False
        );
        Assert.That(inventory.Surfaces.Any(surface => surface.Id.Contains("fake", StringComparison.Ordinal)), Is.False);

        const string Unresolved =
            "namespace Fixture; public sealed class Loader { public void Read(XElement xml) { xml.Element(\"x\"); } }";
        SemanticInventory unresolved = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[] { new CSharpInventoryInput("unresolved.cs", Unresolved) }
            )
        );
        Assert.That(unresolved.Surfaces.Count(surface => surface.Kind == "unresolved-xml-access"), Is.EqualTo(1));

        const string UnresolvedAlias =
            "using X = Missing.XElement; namespace Fixture; public sealed class Loader { public void Read(X xml) { xml.Element(\"x\"); } }";
        SemanticInventory unresolvedAlias = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[] { new CSharpInventoryInput("unresolved-alias.cs", UnresolvedAlias) }
            )
        );
        Assert.That(unresolvedAlias.Surfaces.Count(surface => surface.Kind == "unresolved-xml-access"), Is.EqualTo(1));

        const string UnresolvedAliasChain =
            "using A = Missing.XElement; using B = A; namespace Fixture; public sealed class Loader { public void Read(B xml) { xml.Element(\"x\"); } }";
        SemanticInventory unresolvedAliasChain = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[] { new CSharpInventoryInput("unresolved-chain.cs", UnresolvedAliasChain) }
            )
        );
        Assert.That(
            unresolvedAliasChain.Surfaces.Count(surface => surface.Kind == "unresolved-xml-access"),
            Is.EqualTo(1)
        );
    }

    [Test]
    public void SemanticBranchResolutionSupportsAliasesGlobalQualificationAndUsingStatic()
    {
        const string Source = """
            using BranchAlias = SIL.Machine.Morphology.HermitCrab.SemanticBranch;
            using static SIL.Machine.Morphology.HermitCrab.SemanticBranch;
            namespace Fixture;
            public sealed class SemanticBranch
            {
                public static void Hit(string id) { }
            }
            public sealed class Loader
            {
                public void Run()
                {
                    BranchAlias.Hit("alias");
                    global::SIL.Machine.Morphology.HermitCrab.SemanticBranch.Hit("global");
                    Hit("static");
                    SemanticBranch.Hit("unrelated");
                }
            }
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet("fixture.dtd", Dtd, new[] { new CSharpInventoryInput("fixture.cs", Source) })
        );
        var markers = inventory
            .Surfaces.Where(surface => surface.Kind == "branch-marker")
            .Select(surface => surface.Name)
            .ToArray();
        Assert.That(markers, Is.EquivalentTo(new[] { "alias", "global", "static" }));
    }

    [Test]
    public void DynamicSemanticBranchIdsFailClosed()
    {
        const string Source = """
            using static SIL.Machine.Morphology.HermitCrab.SemanticBranch;
            namespace Fixture;
            public sealed class Loader
            {
                public void Run(string id) { Hit(id); }
            }
            """;

        Assert.Throws<FormatException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("fixture.cs", Source) }
                )
            )
        );

        const string DynamicTarget = """
            using SIL.Machine.Morphology.HermitCrab;
            namespace Fixture;
            public sealed class Loader
            {
                public void Run()
                {
                    dynamic branch = typeof(SemanticBranch);
                    branch.Hit("dynamic");
                }
            }
            """;
        Assert.Throws<FormatException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("dynamic.cs", DynamicTarget) }
                )
            )
        );

        const string UnresolvedTarget =
            "namespace Fixture; public sealed class Loader { public void Run() { SemanticBranch.Hit(\"missing\"); } }";
        Assert.Throws<FormatException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("unresolved.cs", UnresolvedTarget) }
                )
            )
        );

        const string UnresolvedAlias =
            "using SB = Missing.SemanticBranch; namespace Fixture; public sealed class Loader { public void Run() { SB.Hit(\"missing\"); } }";
        Assert.Throws<FormatException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("alias.cs", UnresolvedAlias) }
                )
            )
        );

        const string UnresolvedStatic =
            "using static Missing.SemanticBranch; namespace Fixture; public sealed class Loader { public void Run() { Hit(\"missing\"); } }";
        Assert.Throws<FormatException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("static.cs", UnresolvedStatic) }
                )
            )
        );

        const string UnresolvedAliasChain =
            "using A = Missing.SemanticBranch; using B = A; namespace Fixture; public sealed class Loader { public void Run() { B.Hit(\"missing\"); } }";
        Assert.Throws<FormatException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("alias-chain.cs", UnresolvedAliasChain) }
                )
            )
        );
    }

    [Test]
    public void RuleResolutionUsesExactSymbolsAndRejectsUnresolvedRelevantBases()
    {
        const string Source = """
            using RuleAlias = SIL.Machine.Morphology.HermitCrab.IHCRule;
            namespace Other { public interface IHCRule { } }
            namespace Fixture
            {
                public interface Local : RuleAlias { }
                public abstract class Base : Local { }
                public sealed class Concrete : Base { }
                public sealed class Unrelated : Other.IHCRule { }
            }
            """;
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet("fixture.dtd", Dtd, new[] { new CSharpInventoryInput("fixture.cs", Source) })
        );
        Assert.That(
            inventory.Surfaces.Select(surface => surface.Id),
            Does.Contain("model:rule/Fixture.Concrete/IHCRule")
        );
        Assert.That(
            inventory.Surfaces.Any(surface => surface.Id.Contains("Fixture.Unrelated", StringComparison.Ordinal)),
            Is.False
        );

        const string Broken = "namespace Fixture; public sealed class Broken : IHCRule { }";
        Assert.Throws<FormatException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("broken.cs", Broken) }
                )
            )
        );

        const string BrokenAlias = "using R = Missing.IHCRule; namespace Fixture; public sealed class Broken : R { }";
        Assert.Throws<FormatException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("broken-alias.cs", BrokenAlias) }
                )
            )
        );

        const string BrokenTransitive =
            "namespace Fixture; public interface Local : Missing.IHCRule { } public sealed class Broken : Local { }";
        Assert.Throws<FormatException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("broken-transitive.cs", BrokenTransitive) }
                )
            )
        );

        const string BrokenAliasChain =
            "using A = Missing.IHCRule; using B = A; namespace Fixture; public sealed class Broken : B { }";
        Assert.Throws<FormatException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("broken-alias-chain.cs", BrokenAliasChain) }
                )
            )
        );

        const string ScopedUnrelatedAlias = """
            namespace Other
            {
                using R = Missing.IHCRule;
                public sealed class Unrelated { }
            }
            namespace Fixture
            {
                public interface R { }
                public sealed class Valid : R { }
            }
            """;
        Assert.DoesNotThrow(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("scoped-alias.cs", ScopedUnrelatedAlias) }
                )
            )
        );
    }

    [Test]
    public void SameSignatureLocalFunctionsInDisjointScopesHaveDistinctCanonicalIds()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Loader
            {
                public void Run(bool value)
                {
                    if (value)
                    {
                        void Local() { void Nested() { } Nested(); }
                        Local();
                    }
                    else
                    {
                        void Local() { void Nested() { } Nested(); }
                        Local();
                    }
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
                        "fixture.cs",
                        Source,
                        new[]
                        {
                            "Fixture.Loader",
                            "Fixture.Loader.Run(System.Boolean)/local/Local()#0",
                            "Fixture.Loader.Run(System.Boolean)/local/Local()#1",
                        }
                    ),
                }
            )
        );
        Assert.That(inventory.Surfaces, Has.None.Matches<InventorySurface>(surface => surface.Kind == "callable"));
        Assert.That(
            inventory.Surfaces.Count(surface =>
                surface.Kind == "decision-if" && surface.Parent == "Fixture.Loader.Run(System.Boolean)"
            ),
            Is.EqualTo(2),
            "the internal local-function graph still gives decisions a canonical containing method"
        );
    }

    [Test]
    public void PartialTypeScopeCoversEveryDeclarationAndNestedExecution()
    {
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[]
                {
                    new CSharpInventoryInput(
                        "a.cs",
                        "namespace Fixture; public partial class Loader { public void First(bool value) { if (value) { } } }",
                        new[] { "Fixture.Loader" }
                    ),
                    new CSharpInventoryInput(
                        "b.cs",
                        "namespace Fixture; public partial class Loader { public void Second(bool value) { if (value) { } } }"
                    ),
                }
            )
        );

        var parents = inventory
            .Surfaces.Where(surface => surface.Kind.StartsWith("decision-", StringComparison.Ordinal))
            .Select(surface => surface.Parent)
            .ToArray();
        Assert.That(parents, Does.Contain("Fixture.Loader.First(System.Boolean)"));
        Assert.That(parents, Does.Contain("Fixture.Loader.Second(System.Boolean)"));
    }

    [Test]
    public void PartialMethodDeclarationsMergeToOneCanonicalCallable()
    {
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[]
                {
                    new CSharpInventoryInput(
                        "a.cs",
                        "namespace Fixture; public partial class C { partial void Visit(); public void Run() { Visit(); } }",
                        new[] { "Fixture.C.Visit()" }
                    ),
                    new CSharpInventoryInput(
                        "b.cs",
                        "namespace Fixture; public partial class C { partial void Visit() { if (true) { } } }"
                    ),
                }
            )
        );

        Assert.That(inventory.Surfaces, Has.None.Matches<InventorySurface>(surface => surface.Kind == "callable"));
        Assert.That(
            inventory.Surfaces.Any(surface =>
                surface.Kind.StartsWith("decision-", StringComparison.Ordinal) && surface.Parent == "Fixture.C.Visit()"
            ),
            Is.True
        );
    }

    [Test]
    public void OperatorAccessorAndLocalFunctionDecisionsUseCanonicalContainingSymbols()
    {
        const string Source = """
            namespace Fixture;
            public sealed class C
            {
                private int _value;
                public static C operator +(C left, C right)
                {
                    if (left is null) return right;
                    return left;
                }
                public int Value
                {
                    get { if (_value > 0) return _value; return 0; }
                    set { if (value > 0) _value = value; }
                }
                public void Outer(bool flag)
                {
                    void Local()
                    {
                        if (flag) { }
                    }
                    Local();
                }
            }
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[] { new CSharpInventoryInput("fixture.cs", Source, new[] { "Fixture.C" }) }
            )
        );

        var parents = inventory
            .Surfaces.Where(surface => surface.Kind.StartsWith("decision-", StringComparison.Ordinal))
            .Select(surface => surface.Parent)
            .Where(parent => parent is not null)
            .ToArray();
        Assert.That(parents, Does.Contain("Fixture.C.operator-op_Addition(Fixture.C,Fixture.C)"));
        Assert.That(parents, Does.Contain("Fixture.C.Value/get"));
        Assert.That(parents, Does.Contain("Fixture.C.Value/set"));
        Assert.That(parents, Does.Contain("Fixture.C.Outer(System.Boolean)/local/Local()"));
    }

    [Test]
    public void ExactCanonicalOverloadScopeFiltersDecisions()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Loader
            {
                public void Load(string value) { if (value.Length > 0) { } }
                public void Load(int value) { if (value > 0) { } }
            }
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[] { new CSharpInventoryInput("fixture.cs", Source, new[] { "Fixture.Loader.Load(System.Int32)" }) }
            )
        );

        var parents = inventory
            .Surfaces.Where(surface => surface.Kind.StartsWith("decision-", StringComparison.Ordinal))
            .Select(surface => surface.Parent)
            .Where(parent => parent is not null)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.That(parents, Is.EqualTo(new[] { "Fixture.Loader.Load(System.Int32)" }));
    }

    [Test]
    public void CanonicalCallableIdsIncludeGenericArityAndParameterKinds()
    {
        const string Source = """
            using System.Collections.Generic;
            namespace Fixture;
            public sealed class Signatures
            {
                public void Generic<T>(List<T> values) { }
                public void Ref(ref int value) { }
                public void Out(out int value) { value = 0; }
                public void In(in int value) { }
                public void Params(params int[] values) { }
            }
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[]
                {
                    new CSharpInventoryInput(
                        "fixture.cs",
                        Source,
                        new[]
                        {
                            "Fixture.Signatures",
                            "Fixture.Signatures.Generic`1(System.Collections.Generic.List`1<!!0>)",
                            "Fixture.Signatures.Ref(ref System.Int32)",
                            "Fixture.Signatures.Out(out System.Int32)",
                            "Fixture.Signatures.In(in System.Int32)",
                            "Fixture.Signatures.Params(params System.Int32[])",
                        }
                    ),
                }
            )
        );
        Assert.That(inventory.Surfaces, Has.None.Matches<InventorySurface>(surface => surface.Kind == "callable"));
    }

    [Test]
    public void RealXmlLanguageLoaderTypeScopeEnumeratesThreeLoadOverloads()
    {
        string sourcePath = FindRepositoryFile("src/SIL.Machine.Morphology.HermitCrab/XmlLanguageLoader.cs");
        string source = File.ReadAllText(sourcePath);
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[]
                {
                    new CSharpInventoryInput(
                        "src/SIL.Machine.Morphology.HermitCrab/XmlLanguageLoader.cs",
                        source,
                        new[]
                        {
                            "SIL.Machine.Morphology.HermitCrab.XmlLanguageLoader.Load()",
                            "SIL.Machine.Morphology.HermitCrab.XmlLanguageLoader.Load(System.String)",
                            "SIL.Machine.Morphology.HermitCrab.XmlLanguageLoader.Load(System.String,System.Action`2<System.Exception,System.String>)",
                        }
                    ),
                }
            )
        );

        Assert.That(inventory.Surfaces, Has.None.Matches<InventorySurface>(surface => surface.Kind == "callable"));
        Assert.That(inventory.Surfaces, Has.Some.Matches<InventorySurface>(surface => surface.Kind == "xml-read"));
    }

    private static string FindRepositoryFile(string relativePath)
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            string candidate = Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
                return candidate;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail($"Could not locate repository file '{relativePath}'.");
        return string.Empty;
    }

    [Test]
    public void SourceManifestEnumeratesXmlEnumsRulesMarkersAndAuditedDecisions()
    {
        const string Source = """
            using System;
            using System.Xml.Linq;
            using SIL.Machine.Morphology.HermitCrab;
            using SIL.Machine.Rules;
            using SIL.Machine.Annotations;
            using System.Collections.Generic;
            using System.Linq;
            namespace Fixture;
            public enum Mode { First, Second }
            public interface ILocal : IHCRule { }
            public abstract class AbstractRule : ILocal { }
            public sealed class ConcreteRule : AbstractRule { }
            public sealed class Outer { public sealed class NestedRule : ILocal { } }
            public sealed class GenericRule : IRule<string, string> { }
            public sealed class Loader
            {
                public XElement Load(XElement xml, bool test, int value)
                {
                    XElement one = xml.Element("one");
                    IEnumerable<XElement> many = xml.Elements("many");
                    XAttribute attr = xml.Attribute("isActive");
                    XElement dynamicElement = xml.Element(GetName(value));
                    if (test)
                    {
                        SemanticBranch.Hit("fixture/if");
                    }
                    else if (value > 1)
                    {
                        SemanticBranch.Hit("fixture/else-if");
                    }
                    switch (value)
                    {
                        case 0:
                            break;
                        default: return;
                    }
                    Mode selected = test ? Mode.First : Mode.Second;
                    try { return one; }
                    catch (Exception ex) when (test) { return many.FirstOrDefault(); }
                    for (int i = 0; i < value; i++)
                    {
                        if (i == 2) continue;
                    }

                    return attr is null ? dynamicElement : one;
                }

                private static string GetName(int value) => value.ToString();
            }
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[]
                {
                    new CSharpInventoryInput(
                        "loader.cs",
                        Source,
                        new[]
                        {
                            "Fixture.Loader.Load(System.Xml.Linq.XElement,System.Boolean,System.Int32)",
                            "Fixture.ConcreteRule",
                            "Fixture.GenericRule",
                            "Fixture.Outer.NestedRule",
                        }
                    ),
                }
            )
        );

        var ids = inventory.Surfaces.Select(surface => surface.Id).ToArray();
        Assert.That(
            ids,
            Does.Contain(
                "loader:Fixture.Loader.Load(System.Xml.Linq.XElement,System.Boolean,System.Int32)/Element/one#0"
            )
        );
        Assert.That(
            ids,
            Does.Contain(
                "loader:Fixture.Loader.Load(System.Xml.Linq.XElement,System.Boolean,System.Int32)/Elements/many#0"
            )
        );
        Assert.That(
            ids,
            Does.Contain(
                "loader:Fixture.Loader.Load(System.Xml.Linq.XElement,System.Boolean,System.Int32)/Attribute/isActive#0"
            )
        );
        Assert.That(
            ids,
            Does.Contain(
                "source:dynamic-xml-access/Fixture.Loader.Load(System.Xml.Linq.XElement,System.Boolean,System.Int32)/Element#1"
            )
        );
        Assert.That(ids, Does.Contain("model:enum/Fixture.Mode/First"));
        Assert.That(ids, Does.Contain("model:enum/Fixture.Mode/Second"));
        Assert.That(ids, Does.Contain("model:rule/Fixture.ConcreteRule/IHCRule"));
        Assert.That(ids, Does.Contain("model:rule/Fixture.GenericRule/IRule"));
        Assert.That(ids, Does.Contain("model:rule/Fixture.Outer.NestedRule/IHCRule"));
        Assert.That(ids, Does.Contain("branch:fixture/if"));
        Assert.That(
            ids.Any(id =>
                id.StartsWith(
                    "decision:Fixture.Loader.Load(System.Xml.Linq.XElement,System.Boolean,System.Int32)/if/true#",
                    StringComparison.Ordinal
                )
            )
        );
        Assert.That(
            ids.Any(id =>
                id.StartsWith(
                    "decision:Fixture.Loader.Load(System.Xml.Linq.XElement,System.Boolean,System.Int32)/if/false#",
                    StringComparison.Ordinal
                )
            )
        );
        Assert.That(
            ids.Any(id =>
                id.StartsWith(
                    "decision:Fixture.Loader.Load(System.Xml.Linq.XElement,System.Boolean,System.Int32)/switch/default#",
                    StringComparison.Ordinal
                )
            )
        );
        Assert.That(
            ids.Any(id =>
                id.StartsWith(
                    "decision:Fixture.Loader.Load(System.Xml.Linq.XElement,System.Boolean,System.Int32)/conditional/true#",
                    StringComparison.Ordinal
                )
            )
        );
        Assert.That(
            ids.Any(id =>
                id.StartsWith(
                    "decision:Fixture.Loader.Load(System.Xml.Linq.XElement,System.Boolean,System.Int32)/catch-filter/false#",
                    StringComparison.Ordinal
                )
            )
        );
        Assert.That(
            ids.Any(id =>
                id.StartsWith(
                    "decision:Fixture.Loader.Load(System.Xml.Linq.XElement,System.Boolean,System.Int32)/loop/natural-exit#",
                    StringComparison.Ordinal
                )
            )
        );
        Assert.That(
            ids.Any(id =>
                id.StartsWith(
                    "decision:Fixture.Loader.Load(System.Xml.Linq.XElement,System.Boolean,System.Int32)/loop/continue#",
                    StringComparison.Ordinal
                )
            )
        );
    }

    [Test]
    public void AuditedScopesAreExactAndUnknownScopesFailClosed()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Loader
            {
                public void Load(bool value)
                {
                    if (value) { }
                }
            }
            """;

        SemanticInventory none = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet("fixture.dtd", Dtd, new[] { new CSharpInventoryInput("loader.cs", Source) })
        );
        Assert.That(
            none.Surfaces.Any(surface => surface.Kind.StartsWith("decision-", StringComparison.Ordinal)),
            Is.False
        );

        Assert.Throws<ArgumentException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[]
                    {
                        new CSharpInventoryInput(
                            "loader.cs",
                            Source,
                            new[] { "Fixture.Loader.Missing(System.Boolean)" }
                        ),
                    }
                )
            )
        );
        Assert.Throws<ArgumentException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("loader.cs", Source, new[] { "Fixture.*" }) }
                )
            )
        );
        foreach (
            string malformed in new[] { "Fixture.Loader.Foo[][", "Fixture.Loader.Foo]", "Fixture.Loader.Foo[abc]" }
        )
        {
            Assert.Throws<ArgumentException>(
                () =>
                    SemanticCoverageInventory.Generate(
                        new SemanticCoverageSourceSet(
                            "fixture.dtd",
                            Dtd,
                            new[] { new CSharpInventoryInput("loader.cs", Source, new[] { malformed }) }
                        )
                    ),
                malformed
            );
        }
    }

    [Test]
    public void SourceOrderLineEndingsAndPathsDoNotChangeManifestOrHash()
    {
        const string First = "namespace Fixture {\r\n public enum Mode {\r\n A, B\r\n }\r\n}";
        const string Second = "namespace Other { public enum Mode { C } }";
        SemanticInventory left = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[]
                {
                    new CSharpInventoryInput("z.cs", Second),
                    new CSharpInventoryInput("a.cs", First.Replace("\r\n", "\n", StringComparison.Ordinal)),
                }
            )
        );
        SemanticInventory right = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[] { new CSharpInventoryInput("a.cs", First), new CSharpInventoryInput("z.cs", Second) }
            )
        );

        Assert.That(left.SourceHash, Is.EqualTo(right.SourceHash));
        Assert.That(
            left.Surfaces.Select(surface => surface.Id),
            Is.EqualTo(right.Surfaces.Select(surface => surface.Id))
        );
    }

    [Test]
    public void DuplicatePathsMarkersAndMalformedSourcesAreControlledErrors()
    {
        Assert.Throws<ArgumentException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[]
                    {
                        new CSharpInventoryInput("same.cs", "class A {}"),
                        new CSharpInventoryInput("same.cs", "class B {}"),
                    }
                )
            )
        );

        Assert.Throws<InvalidOperationException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[]
                    {
                        new CSharpInventoryInput(
                            "marker.cs",
                            "using SIL.Machine.Morphology.HermitCrab; class A { void M() { SemanticBranch.Hit(\"same\"); SemanticBranch.Hit(\"same\"); } }"
                        ),
                    }
                )
            )
        );

        Assert.Throws<FormatException>(() =>
            SemanticCoverageInventory.Generate(
                new SemanticCoverageSourceSet(
                    "fixture.dtd",
                    Dtd,
                    new[] { new CSharpInventoryInput("bad.cs", "class A { void M( { }") }
                )
            )
        );
    }

    [Test]
    public void RuleInheritanceAcrossSourceInputsIsEnumerated()
    {
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[]
                {
                    new CSharpInventoryInput(
                        "a.cs",
                        "using SIL.Machine.Morphology.HermitCrab; namespace Fixture { public sealed class CrossRule : LocalRule { } }"
                    ),
                    new CSharpInventoryInput(
                        "b.cs",
                        "using SIL.Machine.Morphology.HermitCrab; namespace Fixture { public class LocalRule : IHCRule { } }"
                    ),
                }
            )
        );

        Assert.That(
            inventory.Surfaces.Select(surface => surface.Id),
            Does.Contain("model:rule/Fixture.CrossRule/IHCRule")
        );
    }

    [Test]
    public async Task SemanticBranchCapturesAreNestedAndAsyncLocal()
    {
        var outer = new HashSet<string>(StringComparer.Ordinal);
        using (SemanticBranch.BeginCapture(outer))
        {
            SemanticBranch.Hit("outer");
            var inner = new HashSet<string>(StringComparer.Ordinal);
            using (SemanticBranch.BeginCapture(inner))
            {
                SemanticBranch.Hit("inner");
            }

            SemanticBranch.Hit("outer-again");
            Assert.That(inner, Is.EquivalentTo(new[] { "inner" }));

            var firstTask = Task.Run(() =>
            {
                var capture = new HashSet<string>(StringComparer.Ordinal);
                using (SemanticBranch.BeginCapture(capture))
                {
                    SemanticBranch.Hit("first");
                }

                return capture;
            });
            var secondTask = Task.Run(() =>
            {
                var capture = new HashSet<string>(StringComparer.Ordinal);
                using (SemanticBranch.BeginCapture(capture))
                {
                    SemanticBranch.Hit("second");
                }

                return capture;
            });
            HashSet<string>[] captures = await Task.WhenAll(firstTask, secondTask);
            Assert.That(captures[0], Is.EquivalentTo(new[] { "first" }));
            Assert.That(captures[1], Is.EquivalentTo(new[] { "second" }));
        }

        Assert.Throws<ArgumentNullException>(() => SemanticBranch.BeginCapture(null!));
        Assert.Throws<ArgumentException>(() => SemanticBranch.Hit(""));
    }

    // HermitCrab compiles under SINGLE_THREADED and OUTPUT_ANALYSES; a single-configuration
    // census cannot see a decision that only exists when a symbol is defined.
    [Test]
    public void DecisionsInsideEveryConditionalConfigurationAreCensused()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Loader
            {
                public void Load(bool value)
            #if SINGLE_THREADED
                    {
                        if (value) { }
                    }
            #else
                    {
                        while (value) { }
                    }
            #endif
                public void Report(bool value)
                {
            #if OUTPUT_ANALYSES
                    switch (value) { default: break; }
            #endif
                }
            }
            """;

        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[] { new CSharpInventoryInput("loader.cs", Source, new[] { "Fixture.Loader" }) }
            )
        );

        string[] kinds = inventory
            .Surfaces.Where(surface => surface.Kind.StartsWith("decision-", StringComparison.Ordinal))
            .Select(surface => surface.Kind)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(kind => kind, StringComparer.Ordinal)
            .ToArray();

        Assert.That(kinds, Does.Contain("decision-loop"), "the !SINGLE_THREADED branch must be censused");
        Assert.That(kinds, Does.Contain("decision-if"), "the SINGLE_THREADED branch must be censused");
        Assert.That(kinds, Does.Contain("decision-switch"), "the OUTPUT_ANALYSES branch must be censused");
    }

    // A configuration-only decision sharing a parent and kind with an unconditional one
    // shifts per-configuration ordinals, so IDs must be assigned after the union.
    private const string SharedGroupSource = """
        namespace Fixture;
        public sealed class Loader
        {
            public void Load(bool value)
            {
        #if SINGLE_THREADED
                if (value) { }
        #endif
                if (!value) { }
            }
        }
        """;

    [Test]
    public void ConfigurationOnlyDecisionsDoNotDuplicateSharedOrdinalGroups()
    {
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[] { new CSharpInventoryInput("loader.cs", SharedGroupSource, new[] { "Fixture.Loader" }) }
            )
        );

        InventorySurface[] taken = inventory
            .Surfaces.Where(surface => surface.Name == "Fixture.Loader.Load(System.Boolean)/if/true")
            .ToArray();

        Assert.That(taken, Has.Length.EqualTo(2), "each source decision must yield exactly one surface");
        Assert.That(
            taken
                .Select(surface => surface.Id.Substring(surface.Id.LastIndexOf('-') + 1))
                .Distinct(StringComparer.Ordinal)
                .Count(),
            Is.EqualTo(2),
            "the two decisions must keep distinct fingerprints"
        );
        Assert.That(
            taken.Select(surface => surface.Id.Split('#')[1].Split('-')[0]).Distinct(StringComparer.Ordinal).Count(),
            Is.EqualTo(2),
            "one ordinal per decision, assigned over the unioned candidate set"
        );
    }

    [Test]
    public void SurfacesRecordTheConfigurationsThatContainThem()
    {
        const string Everywhere = "OUTPUT_ANALYSES,OUTPUT_ANALYSES+SINGLE_THREADED,SINGLE_THREADED,base";
        const string SingleThreadedOnly = "OUTPUT_ANALYSES+SINGLE_THREADED,SINGLE_THREADED";
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[] { new CSharpInventoryInput("loader.cs", SharedGroupSource, new[] { "Fixture.Loader" }) }
            )
        );

        Assert.That(inventory.Surfaces, Has.None.Matches<InventorySurface>(surface => surface.Kind == "callable"));

        string[] availability = inventory
            .Surfaces.Where(surface => surface.Name == "Fixture.Loader.Load(System.Boolean)/if/true")
            .Select(surface => surface.Configurations)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.That(availability, Is.EqualTo(new[] { SingleThreadedOnly, Everywhere }));

        InventorySurface dtdSurface = inventory.Surfaces.First(surface =>
            surface.Source.StartsWith("fixture.dtd", StringComparison.Ordinal)
        );
        Assert.That(dtdSurface.Configurations, Is.Empty, "DTD surfaces are not configuration-scoped");
    }

    [Test]
    public void RealMorpherExposesConfigurationOnlyDecisionsAndTheirConfigurations()
    {
        const string Relative = "src/SIL.Machine.Morphology.HermitCrab/Morpher.cs";
        // Scoped to the type rather than to one method signature. This is a real engine file, so an
        // upstream rename must not be able to present itself here as a census defect.
        const string Scope = "SIL.Machine.Morphology.HermitCrab.Morpher";
        const string OutputAnalysesOnly = "OUTPUT_ANALYSES,OUTPUT_ANALYSES+SINGLE_THREADED";
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[]
                {
                    new CSharpInventoryInput(Relative, File.ReadAllText(FindRepositoryFile(Relative)), new[] { Scope }),
                }
            )
        );

        Assert.That(inventory.Surfaces, Has.None.Matches<InventorySurface>(surface => surface.Kind == "callable"));

        InventorySurface[] decisions = inventory
            .Surfaces.Where(surface =>
                surface.Kind.StartsWith("decision-", StringComparison.Ordinal)
                && string.Equals(surface.Configurations, OutputAnalysesOnly, StringComparison.Ordinal)
            )
            .ToArray();
        Assert.That(
            decisions,
            Is.Not.Empty,
            "a configuration-only region's decisions must still be censused, under the configurations that contain them"
        );
    }

    [Test]
    public void CallablesRemainInternalWhileAuditedDecisionsRemainSemanticSurfaces()
    {
        const string Source = """
            namespace Fixture;
            public sealed class Loader
            {
                public void Run(bool value)
                {
                    Helper();
                    if (value) Helper();
                }

                private static void Helper() { }
            }
            """;
        SemanticInventory inventory = SemanticCoverageInventory.Generate(
            new SemanticCoverageSourceSet(
                "fixture.dtd",
                Dtd,
                new[] { new CSharpInventoryInput("fixture.cs", Source, new[] { "Fixture.Loader" }) }
            )
        );

        Assert.Multiple(() =>
        {
            Assert.That(inventory.Surfaces, Has.None.Matches<InventorySurface>(surface => surface.Kind == "callable"));
            Assert.That(
                inventory.Surfaces.Count(surface =>
                    surface.Kind == "decision-if" && surface.Parent == "Fixture.Loader.Run(System.Boolean)"
                ),
                Is.EqualTo(2),
                "the internal callable graph must still provide canonical parents and audited decision discovery"
            );
        });
    }
}
