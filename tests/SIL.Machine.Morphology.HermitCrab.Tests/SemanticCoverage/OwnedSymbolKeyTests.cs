using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class OwnedSymbolKeyTests
{
    private static string RepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }
        Assert.Fail("Could not locate the repository root.");
        return string.Empty;
    }

    [Test]
    [CancelAfter(120_000)]
    public async Task LiveBridgeRekeysRetargetedSymbolsToProfileLocalOwnedDefinitions()
    {
        string root = RepositoryRoot();
        RepositoryCompilationGraph captured = await new RepositoryCompilationGraphLoader(
            new MsBuildProcessRunner()
        ).LoadAsync(new RepositoryRoot(root), CancellationToken.None);
        RoslynCompilationGraph graph = RoslynCompilationGraph.Build(captured);
        OwnedSymbolBridge bridge = OwnedSymbolBridge.Create(graph);

        INamedTypeSymbol machineDefinition = Type(graph, "machine", "base", "SIL.Machine.Rules.IRule`2");
        INamedTypeSymbol machineFromHc = Type(graph, "hc", "base", "SIL.Machine.Rules.IRule`2");
        OwnedSymbolKey machineKey = bridge.KeyFor("base", machineDefinition);

        INamedTypeSymbol toolDefinition = Type(
            graph,
            "hc-tool",
            "base",
            "SIL.Machine.Morphology.HermitCrab.SignatureFormat"
        );
        INamedTypeSymbol toolFromConformance = Type(
            graph,
            "hc-conformance",
            "base",
            "SIL.Machine.Morphology.HermitCrab.SignatureFormat"
        );
        OwnedSymbolKey toolKey = bridge.KeyFor("base", toolDefinition);

        INamedTypeSymbol conformanceDefinition = Type(
            graph,
            "hc-conformance",
            "base",
            "SIL.Machine.Morphology.HermitCrab.Conformance.Fixture"
        );
        OwnedSymbolKey conformanceKey = bridge.KeyFor("base", conformanceDefinition);
        IMethodSymbol idGetter = conformanceDefinition.GetMembers("Id").OfType<IPropertySymbol>().Single().GetMethod!;
        OwnedSymbolKey idGetterKey = bridge.KeyFor("base", idGetter);

        RoslynCompilationNode hcNode = graph.Nodes.Values.Single(item =>
            item.Key.ProjectId == "hc" && item.Key.ProfileId == "base"
        );
        SyntaxTree morpherTree = hcNode.Compilation.SyntaxTrees.Single(tree =>
            tree.FilePath.EndsWith($"{Path.DirectorySeparatorChar}Morpher.cs", StringComparison.OrdinalIgnoreCase)
        );
        SemanticModel morpherModel = hcNode.Compilation.GetSemanticModel(morpherTree);
        LocalFunctionStatementSyntax localSyntax = morpherTree
            .GetRoot()
            .DescendantNodes()
            .OfType<LocalFunctionStatementSyntax>()
            .Single(node => node.Identifier.ValueText == "GenerateSynthesis");
        IMethodSymbol local = morpherModel.GetDeclaredSymbol(localSyntax)!;
        OwnedSymbolKey localKey = bridge.KeyFor("base", local);
        IMethodSymbol[] lambdas = morpherTree
            .GetRoot()
            .DescendantNodes()
            .OfType<AnonymousFunctionExpressionSyntax>()
            .Select(node => (morpherModel.GetOperation(node) as IAnonymousFunctionOperation)?.Symbol)
            .Where(symbol => symbol is not null)
            .Cast<IMethodSymbol>()
            .Take(2)
            .ToArray();
        OwnedSymbolKey[] lambdaKeys = lambdas.Select(symbol => bridge.KeyFor("base", symbol)).ToArray();
        IFieldSymbol analysisRuleField = Type(graph, "hc", "base", "SIL.Machine.Morphology.HermitCrab.Morpher")
            .GetMembers("_analysisRule")
            .OfType<IFieldSymbol>()
            .Single();
        INamedTypeSymbol retargetedConstructedRule = (INamedTypeSymbol)analysisRuleField.Type;
        OwnedSymbolKey constructedRuleKey = bridge.KeyFor("base", retargetedConstructedRule);
        INamedTypeSymbol resolvedConstructedRule = (INamedTypeSymbol)bridge.Resolve("base", constructedRuleKey);

        Assert.Multiple(() =>
        {
            Assert.That(bridge.KeyFor("base", machineFromHc), Is.EqualTo(machineKey));
            Assert.That(
                SymbolEqualityComparer.Default.Equals(bridge.Resolve("base", machineKey), machineDefinition),
                Is.True
            );
            Assert.That(bridge.KeyFor("base", toolFromConformance), Is.EqualTo(toolKey));
            Assert.That(
                SymbolEqualityComparer.Default.Equals(bridge.Resolve("base", toolKey), toolDefinition),
                Is.True
            );
            Assert.That(
                SymbolEqualityComparer.Default.Equals(bridge.Resolve("base", conformanceKey), conformanceDefinition),
                Is.True
            );
            Assert.That(SymbolEqualityComparer.Default.Equals(bridge.Resolve("base", idGetterKey), idGetter), Is.True);
            Assert.That(
                idGetterKey.Value,
                Does.EndWith("/SIL.Machine.Morphology.HermitCrab.Conformance.Fixture.Id/get")
            );
            Assert.That(SymbolEqualityComparer.Default.Equals(bridge.Resolve("base", localKey), local), Is.True);
            Assert.That(localKey.Value, Does.Contain(".GenerateWords("));
            Assert.That(localKey.Value, Does.Contain("/local/GenerateSynthesis("));
            Assert.That(lambdaKeys.Select(key => key.Value), Is.Unique);
            Assert.That(lambdaKeys.All(key => key.Value.Contains("/lambda@", StringComparison.Ordinal)), Is.True);
            Assert.That(lambdaKeys.All(key => bridge.Resolve("base", key) is IMethodSymbol), Is.True);
            Assert.That(
                resolvedConstructedRule.ContainingAssembly,
                Is.SameAs(
                    graph
                        .Nodes.Values.Single(item => item.Key.ProjectId == "machine" && item.Key.ProfileId == "base")
                        .Compilation.Assembly
                )
            );
            Assert.That(resolvedConstructedRule, Is.Not.SameAs(retargetedConstructedRule));
            Assert.That(machineKey.Value, Does.StartWith("owned:machine/"));
            Assert.That(toolKey.Value, Does.StartWith("owned:hc-tool/"));
            Assert.That(conformanceKey.Value, Does.StartWith("owned:hc-conformance/"));
            Assert.That(() => bridge.KeyFor("combined", machineDefinition), Throws.TypeOf<CompilerInputException>());
        });
    }

    [Test]
    public void CanonicalKeysCoverEveryMemberShapeWithoutCollision()
    {
        const string Source = """
            namespace Fixture;
            public class Outer<T>
            {
                public class Inner<V> { }
                public int Field;
                public event System.Action? Changed;
                public string Property => "";
                public int this[int index] => index;
                public Outer(ref int value, out string text) { text = ""; }
                public U Method<U>(T[] values, ref U item, out int count, in string name, params object[] rest)
                { count = 0; return item; }
                public static Outer<T> operator +(Outer<T> left, Outer<T> right) => left;
                public static implicit operator int(Outer<T> value) => 0;
                public static implicit operator long(Outer<T> value) => 0;
            }
            """;
        SyntaxTree tree = CSharpSyntaxTree.ParseText(Source, path: "repo:/fixture.cs");
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Fixture.Assembly",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        INamedTypeSymbol type = compilation.GetTypeByMetadataName("Fixture.Outer`1")!;
        ISymbol namespaceSymbol = compilation
            .GetSemanticModel(tree)
            .GetDeclaredSymbol(
                tree.GetRoot().DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().Single()
            )!;
        IMethodSymbol method = type.GetMembers("Method").OfType<IMethodSymbol>().Single();
        IMethodSymbol constructor = type.InstanceConstructors.Single(item => !item.IsImplicitlyDeclared);
        IMethodSymbol addition = type.GetMembers()
            .OfType<IMethodSymbol>()
            .Single(item => item.MethodKind == MethodKind.UserDefinedOperator);
        IMethodSymbol[] conversions = type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(item => item.MethodKind == MethodKind.Conversion)
            .ToArray();
        IPropertySymbol property = type.GetMembers("Property").OfType<IPropertySymbol>().Single();
        IPropertySymbol indexer = type.GetMembers().OfType<IPropertySymbol>().Single(item => item.IsIndexer);
        IFieldSymbol field = type.GetMembers("Field").OfType<IFieldSymbol>().Single();
        IEventSymbol @event = type.GetMembers("Changed").OfType<IEventSymbol>().Single();
        INamedTypeSymbol innerDefinition = type.GetTypeMembers("Inner").Single();
        INamedTypeSymbol outerOfInt = type.Construct(compilation.GetSpecialType(SpecialType.System_Int32));
        INamedTypeSymbol outerOfLong = type.Construct(compilation.GetSpecialType(SpecialType.System_Int64));
        INamedTypeSymbol innerOfIntString = outerOfInt
            .GetTypeMembers("Inner")
            .Single()
            .Construct(compilation.GetSpecialType(SpecialType.System_String));
        INamedTypeSymbol innerOfLongString = outerOfLong
            .GetTypeMembers("Inner")
            .Single()
            .Construct(compilation.GetSpecialType(SpecialType.System_String));

        OwnedSymbolKey[] keys = new[]
        {
            namespaceSymbol,
            type,
            constructor,
            method,
            addition,
            property,
            indexer,
            field,
            @event,
        }
            .Concat(conversions)
            .Select(symbol => OwnedSymbolKey.Create("fixture", symbol))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(keys.Select(key => key.Value), Is.Unique);
            Assert.That(OwnedSymbolKey.Create("fixture", type).Value, Does.EndWith("/Fixture.Outer`1"));
            Assert.That(
                OwnedSymbolKey.Create("fixture", method).Value,
                Does.EndWith(
                    "/Fixture.Outer`1.Method`1(!0[],ref !!0,out System.Int32,in System.String,params System.Object[])"
                )
            );
            Assert.That(
                OwnedSymbolKey.Create("fixture", constructor).Value,
                Does.EndWith("/Fixture.Outer`1..ctor(ref System.Int32,out System.String)")
            );
            Assert.That(
                OwnedSymbolKey.Create("fixture", indexer).Value,
                Does.EndWith("/Fixture.Outer`1.this(System.Int32)")
            );
            Assert.That(OwnedSymbolKey.Create("fixture", addition).Value, Does.Contain("operator-op_Addition"));
            Assert.That(
                OwnedSymbolKey.Create("fixture", conversions[0]),
                Is.Not.EqualTo(OwnedSymbolKey.Create("fixture", conversions[1]))
            );
            Assert.That(
                OwnedSymbolKey.Create("fixture", innerOfIntString),
                Is.Not.EqualTo(OwnedSymbolKey.Create("fixture", innerOfLongString))
            );
            Assert.That(
                OwnedSymbolKey.Create("fixture", innerDefinition).Value,
                Does.EndWith("/Fixture.Outer`1.Inner`1")
            );
            Assert.That(OwnedSymbolKey.Create("fixture", namespaceSymbol).Value, Does.EndWith("/Fixture"));
        });
    }

    private static INamedTypeSymbol Type(
        RoslynCompilationGraph graph,
        string projectId,
        string profileId,
        string metadataName
    )
    {
        RoslynCompilationNode node = graph.Nodes.Values.Single(item =>
            item.Key.ProjectId == projectId && item.Key.ProfileId == profileId
        );
        return node.Compilation.GetTypeByMetadataName(metadataName)
            ?? throw new AssertionException($"Type '{metadataName}' was not found in '{node.Key}'.");
    }
}
