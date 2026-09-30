using System.Collections.Immutable;
using System.Text;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class CompilationGraphHashingTests
{
    [Test]
    public void ComputesThreeIndependentLowercaseSha256Domains()
    {
        CompilationGraphHashInputs baseline = Inputs();
        GraphHashes hashes = CompilationGraphHashing.Compute(baseline);

        Assert.Multiple(() =>
        {
            Assert.That(hashes.GraphInputHash, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(hashes.ToolchainHash, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(hashes.GraphHash, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(hashes.GraphInputHash, Is.Not.EqualTo(hashes.ToolchainHash));
            Assert.That(hashes.GraphHash, Is.Not.EqualTo(hashes.GraphInputHash));
        });

        GraphHashes sourceChanged = CompilationGraphHashing.Compute(Inputs(sourceText: "class C { int X; }\n"));
        GraphHashes toolchainChanged = CompilationGraphHashing.Compute(Inputs(roslynText: "roslyn-v2"));
        GraphHashes edgeChanged = CompilationGraphHashing.Compute(Inputs(edgeTarget: "other-owned-project"));

        Assert.Multiple(() =>
        {
            Assert.That(sourceChanged.GraphInputHash, Is.Not.EqualTo(hashes.GraphInputHash));
            Assert.That(sourceChanged.ToolchainHash, Is.EqualTo(hashes.ToolchainHash));
            Assert.That(toolchainChanged.ToolchainHash, Is.Not.EqualTo(hashes.ToolchainHash));
            Assert.That(toolchainChanged.GraphInputHash, Is.EqualTo(hashes.GraphInputHash));
            Assert.That(edgeChanged.GraphHash, Is.Not.EqualTo(hashes.GraphHash));
        });
    }

    [Test]
    public void TextEolAndPhysicalRootRelocationDoNotChangeHashes()
    {
        GraphHashFile windowsSource = File("repo:/src/C.cs", "class C {\r\n}\r\n", GraphHashFileKind.Text);
        GraphHashFile linuxSource = File("repo:/src/C.cs", "class C {\n}\n", GraphHashFileKind.Text);

        GraphHashes windows = CompilationGraphHashing.Compute(Inputs(source: windowsSource));
        GraphHashes linux = CompilationGraphHashing.Compute(Inputs(source: linuxSource));

        Assert.That(windows, Is.EqualTo(linux));
        Assert.Multiple(() =>
        {
            Assert.That(
                LogicalPathTokens.FromAbsolute(
                    @"C:\checkout\src\C.cs",
                    new LogicalPathRoots(@"C:\checkout", @"C:\sdk", @"C:\nuget", @"C:\generated")
                ),
                Is.EqualTo("repo:/src/C.cs")
            );
            Assert.That(
                LogicalPathTokens.FromAbsolute(
                    "/mnt/checkout/src/C.cs",
                    new LogicalPathRoots("/mnt/checkout", "/opt/sdk", "/home/me/.nuget", "/tmp/generated")
                ),
                Is.EqualTo("repo:/src/C.cs")
            );
        });
    }

    [Test]
    public void AncestorEditorConfigFilesAreAdmittedWithALocationIndependentLogicalIdentity()
    {
        var roots = new LogicalPathRoots(
            @"C:\outer\machine\worktrees\squashed",
            @"C:\sdk",
            @"C:\nuget",
            @"C:\generated"
        );

        string logical = LogicalPathTokens.FromAbsoluteAdmittingAncestorEditorConfig(
            @"C:\outer\machine\.editorconfig",
            roots
        );

        string unixShaped = LogicalPathTokens.FromAbsoluteAdmittingAncestorEditorConfig(
            "/outer/machine/.editorconfig",
            new LogicalPathRoots("/outer/machine/worktrees/squashed", "/opt/sdk", "/home/me/.nuget", "/tmp/generated")
        );

        Assert.Multiple(() =>
        {
            Assert.That(logical, Is.EqualTo("ancestor-editorconfig:/2"));
            Assert.That(LogicalPathTokens.IsLogicalPath(logical), Is.True);
            Assert.That(unixShaped, Is.EqualTo(logical));
        });
    }

    [Test]
    public void AncestorEditorConfigIdentityAndHashSurviveRelocationOfBothTheOuterCheckoutAndTheWorktree()
    {
        var hereRoots = new LogicalPathRoots(
            @"C:\Users\dev1\machine\.worktrees\squashed",
            @"C:\sdk",
            @"C:\nuget",
            @"C:\generated"
        );
        var thereRoots = new LogicalPathRoots(
            @"D:\ci\agent7\machine\.worktrees\squashed",
            @"D:\sdk",
            @"D:\nuget",
            @"D:\generated"
        );

        string hereLogical = LogicalPathTokens.FromAbsoluteAdmittingAncestorEditorConfig(
            @"C:\Users\dev1\machine\.editorconfig",
            hereRoots
        );
        string thereLogical = LogicalPathTokens.FromAbsoluteAdmittingAncestorEditorConfig(
            @"D:\ci\agent7\machine\.editorconfig",
            thereRoots
        );

        Assert.That(hereLogical, Is.EqualTo(thereLogical));

        GraphHashFile hereFile = File(hereLogical, "root = true\n", GraphHashFileKind.Text);
        GraphHashFile thereFile = File(thereLogical, "root = true\n", GraphHashFileKind.Text);
        Assert.That(
            CompilationGraphHashing.Compute(InputsWithEditorConfig(hereFile)),
            Is.EqualTo(CompilationGraphHashing.Compute(InputsWithEditorConfig(thereFile)))
        );
    }

    [Test]
    public void FromAbsoluteAdmittingAncestorEditorConfigStillFailsClosedOutsideTheEditorConfigCase()
    {
        var roots = new LogicalPathRoots(
            @"C:\outer\machine\worktrees\squashed",
            @"C:\sdk",
            @"C:\nuget",
            @"C:\generated"
        );

        Assert.That(
            () =>
                LogicalPathTokens.FromAbsoluteAdmittingAncestorEditorConfig(
                    @"C:\outer\machine\Directory.Build.props",
                    roots
                ),
            Throws.TypeOf<InvalidDataException>()
        );
    }

    [Test]
    public void CompilerOrderIsSemanticButSetOrderIsNot()
    {
        CompilationGraphHashInputs baseline = Inputs(
            arguments: new[] { new OrderedHashValue(0, "/define:A"), new OrderedHashValue(1, "/nullable:enable") },
            profileSymbols: new[] { "A", "B" }
        );
        CompilationGraphHashInputs reorderedArguments = Inputs(
            arguments: new[] { new OrderedHashValue(0, "/nullable:enable"), new OrderedHashValue(1, "/define:A") },
            profileSymbols: new[] { "A", "B" }
        );
        CompilationGraphHashInputs reorderedSet = Inputs(
            arguments: new[] { new OrderedHashValue(0, "/define:A"), new OrderedHashValue(1, "/nullable:enable") },
            profileSymbols: new[] { "B", "A" }
        );

        Assert.That(
            CompilationGraphHashing.Compute(reorderedArguments).GraphInputHash,
            Is.Not.EqualTo(CompilationGraphHashing.Compute(baseline).GraphInputHash)
        );
        Assert.That(
            CompilationGraphHashing.Compute(reorderedSet),
            Is.EqualTo(CompilationGraphHashing.Compute(baseline))
        );
    }

    [Test]
    public void LogicalPathsRejectUnknownRootsAndCaseCollisions()
    {
        var roots = new LogicalPathRoots(@"C:\repo", @"C:\sdk", @"C:\nuget", @"C:\generated");

        Assert.That(
            () => LogicalPathTokens.FromAbsolute(@"C:\elsewhere\file.cs", roots),
            Throws.TypeOf<InvalidDataException>()
        );
        Assert.That(
            () => LogicalPathTokens.ValidateUnique(new[] { "repo:/src/C.cs", "repo:/src/c.cs" }),
            Throws.TypeOf<InvalidDataException>()
        );
    }

    [Test]
    public void ExactLogicalFileReuseAcrossProfilesIsAllowedButCaseVariantsAreRejected()
    {
        GraphHashFile source = File("repo:/src/C.cs", "class C { }\n", GraphHashFileKind.Text);
        NodeHashInput baseNode = Node("base", source);
        NodeHashInput alternateNode = Node("single-threaded", source);

        Assert.DoesNotThrow(() =>
            new CompilationGraphHashInputs(
                "hc-compilation-graph/v1",
                new[] { new ProjectHashInput("hc", "repo:/src/hc.csproj", "netstandard2.0") },
                new[]
                {
                    new ProfileHashInput("base", Array.Empty<string>()),
                    new ProfileHashInput("single-threaded", new[] { "SINGLE_THREADED" }),
                },
                new[] { baseNode, alternateNode },
                Array.Empty<RepositoryProjectEdge>(),
                Toolchain(),
                File("repo:/eng/HcSemanticCompilerInputs.targets", "<Project />\n", GraphHashFileKind.Text),
                new OptionalGraphHashFile(false, null)
            )
        );

        GraphHashFile caseVariant = File("repo:/src/c.cs", "class C { }\n", GraphHashFileKind.Text);
        Assert.That(
            () =>
                new CompilationGraphHashInputs(
                    "hc-compilation-graph/v1",
                    new[] { new ProjectHashInput("hc", "repo:/src/hc.csproj", "netstandard2.0") },
                    new[]
                    {
                        new ProfileHashInput("base", Array.Empty<string>()),
                        new ProfileHashInput("single-threaded", new[] { "SINGLE_THREADED" }),
                    },
                    new[] { baseNode, Node("single-threaded", caseVariant) },
                    Array.Empty<RepositoryProjectEdge>(),
                    Toolchain(),
                    File("repo:/eng/HcSemanticCompilerInputs.targets", "<Project />\n", GraphHashFileKind.Text),
                    new OptionalGraphHashFile(false, null)
                ),
            Throws.TypeOf<InvalidDataException>()
        );
    }

    [Test]
    public void OrderedInputsRequireUniqueContiguousOrdinals()
    {
        GraphHashFile source = File("repo:/src/C.cs", "class C { }\n", GraphHashFileKind.Text);
        Assert.That(
            () => Node("base", source, arguments: new[] { new OrderedHashValue(0, "a"), new OrderedHashValue(0, "b") }),
            Throws.TypeOf<InvalidDataException>()
        );
        Assert.That(
            () => Node("base", source, arguments: new[] { new OrderedHashValue(0, "a"), new OrderedHashValue(2, "b") }),
            Throws.TypeOf<InvalidDataException>()
        );
        Assert.That(
            () =>
                new NodeHashInput(
                    new RepositoryGraphNodeKey("hc", "netstandard2.0", "base"),
                    new Dictionary<string, string>(),
                    Array.Empty<OrderedHashValue>(),
                    new[] { new OrderedGraphHashFile(1, source) },
                    Array.Empty<ReferenceHashInput>(),
                    Array.Empty<ProjectReferenceHashInput>(),
                    Array.Empty<AnalyzerHashInput>(),
                    Array.Empty<GraphHashFile>(),
                    Array.Empty<GraphHashFile>(),
                    Array.Empty<GraphHashFile>(),
                    Array.Empty<GraphHashFile>(),
                    Array.Empty<GraphHashFile>()
                ),
            Throws.TypeOf<InvalidDataException>()
        );
    }

    [Test]
    public void OverlappingPhysicalRootsAreRejectedAsAmbiguous()
    {
        Assert.That(
            () => new LogicalPathRoots(@"C:\repo", @"C:\sdk", @"C:\nuget", @"C:\repo\obj\generated"),
            Throws.TypeOf<InvalidDataException>()
        );
    }

    [Test]
    public void TraversalSegmentsAreRejectedAndJsonStringEolIsNormalized()
    {
        var roots = new LogicalPathRoots(@"C:\repo", @"C:\sdk", @"C:\nuget", @"C:\generated");
        Assert.That(
            () => LogicalPathTokens.FromAbsolute(@"C:\repo\sub\..\C.cs", roots),
            Throws.TypeOf<InvalidDataException>()
        );

        GraphHashFile windowsJson = File(
            "repo:/obj/project.assets.json",
            "{\"text\":\"a\\r\\nb\"}",
            GraphHashFileKind.Json
        );
        GraphHashFile linuxJson = File("repo:/obj/project.assets.json", "{\"text\":\"a\\nb\"}", GraphHashFileKind.Json);
        Assert.That(
            CompilationGraphHashing.Compute(InputsWithAsset(windowsJson)),
            Is.EqualTo(CompilationGraphHashing.Compute(InputsWithAsset(linuxJson)))
        );
    }

    [Test]
    public void ProjectPathsRequireLogicalTokensAndShareTheGlobalCollisionDomain()
    {
        Assert.That(
            () => new ProjectHashInput("hc", @"C:\repo\src\hc.csproj", "netstandard2.0"),
            Throws.TypeOf<ArgumentException>()
        );
        Assert.That(
            () => new ProjectHashInput("hc", "other:/src/hc.csproj", "netstandard2.0"),
            Throws.TypeOf<ArgumentException>()
        );

        Assert.That(
            () =>
                new CompilationGraphHashInputs(
                    "hc-compilation-graph/v1",
                    new[] { new ProjectHashInput("hc", "repo:/SRC/C.cs", "netstandard2.0") },
                    new[] { new ProfileHashInput("base", Array.Empty<string>()) },
                    new[] { Node("base", File("repo:/src/C.cs", "class C { }\n", GraphHashFileKind.Text)) },
                    Array.Empty<RepositoryProjectEdge>(),
                    Toolchain(),
                    File("repo:/eng/HcSemanticCompilerInputs.targets", "<Project />\n", GraphHashFileKind.Text),
                    new OptionalGraphHashFile(false, null)
                ),
            Throws.TypeOf<InvalidDataException>()
        );

        Assert.DoesNotThrow(() =>
            new CompilationGraphHashInputs(
                "hc-compilation-graph/v1",
                new[] { new ProjectHashInput("hc", "repo:/src/hc.csproj", "netstandard2.0") },
                new[] { new ProfileHashInput("base", Array.Empty<string>()) },
                new[]
                {
                    new NodeHashInput(
                        new RepositoryGraphNodeKey("hc", "netstandard2.0", "base"),
                        new Dictionary<string, string>(),
                        Array.Empty<OrderedHashValue>(),
                        Array.Empty<OrderedGraphHashFile>(),
                        Array.Empty<ReferenceHashInput>(),
                        Array.Empty<ProjectReferenceHashInput>(),
                        Array.Empty<AnalyzerHashInput>(),
                        Array.Empty<GraphHashFile>(),
                        Array.Empty<GraphHashFile>(),
                        Array.Empty<GraphHashFile>(),
                        Array.Empty<GraphHashFile>(),
                        new[] { File("repo:/src/hc.csproj", "<Project />\n", GraphHashFileKind.Text) }
                    ),
                },
                Array.Empty<RepositoryProjectEdge>(),
                Toolchain(),
                File("repo:/eng/HcSemanticCompilerInputs.targets", "<Project />\n", GraphHashFileKind.Text),
                new OptionalGraphHashFile(false, null)
            )
        );
    }

    [Test]
    public void FilesMapCorrectlyWhenAnAdmittedRootIsAFileSystemRoot()
    {
        Assert.That(
            LogicalPathTokens.FromAbsolute(
                @"C:\repo\file.cs",
                new LogicalPathRoots(@"C:\", @"D:\sdk", @"D:\nuget", @"D:\generated")
            ),
            Is.EqualTo("repo:/repo/file.cs")
        );
        Assert.That(
            LogicalPathTokens.FromAbsolute("/repo/file.cs", new LogicalPathRoots("/", "/sdk", "/nuget", "/generated")),
            Is.EqualTo("repo:/repo/file.cs")
        );
    }

    [Test]
    public void HashModelsRejectNullElementsDuplicateIdentitiesAndAmbiguousNodeSegments()
    {
        Assert.That(() => new ProfileHashInput("base", new string[] { null! }), Throws.ArgumentException);
        Assert.That(
            () =>
                new ToolchainHashInput("sdk", "msbuild", "roslyn", "compiler", "loader", new GraphHashFile[] { null! }),
            Throws.ArgumentException
        );

        CompilationGraphHashInputs baseline = Inputs();
        Assert.That(
            () =>
                new CompilationGraphHashInputs(
                    baseline.SchemaVersion,
                    baseline.Projects.Concat(baseline.Projects).ToArray(),
                    baseline.Profiles,
                    baseline.Nodes,
                    baseline.Edges,
                    baseline.Toolchain,
                    baseline.CaptureTarget,
                    baseline.LockFile
                ),
            Throws.TypeOf<InvalidDataException>()
        );
        Assert.That(
            () =>
                new CompilationGraphHashInputs(
                    baseline.SchemaVersion,
                    baseline.Projects,
                    baseline.Profiles,
                    baseline.Nodes.Concat(baseline.Nodes).ToArray(),
                    baseline.Edges,
                    baseline.Toolchain,
                    baseline.CaptureTarget,
                    baseline.LockFile
                ),
            Throws.TypeOf<InvalidDataException>()
        );
        Assert.That(() => new RepositoryGraphNodeKey("hc/other", "netstandard2.0", "base"), Throws.ArgumentException);
    }

    [Test]
    public void CanonicalJsonRejectsDuplicatePropertiesAndNormalizesEquivalentNumbers()
    {
        Assert.That(
            () => CanonicalJson.NormalizeJson(Encoding.UTF8.GetBytes("{\"a\":1,\"a\":2}")),
            Throws.TypeOf<InvalidDataException>()
        );
        Assert.That(
            CanonicalJson.NormalizeJson(Encoding.UTF8.GetBytes("{\"n\":1}")),
            Is.EqualTo(CanonicalJson.NormalizeJson(Encoding.UTF8.GetBytes("{\"n\":1.0}")))
        );
    }

    [Test]
    public void NullLogicalPathsAndDuplicateFilesystemRootsFailClosed()
    {
        Assert.That(
            () => new GraphHashFile(null!, ImmutableArray<byte>.Empty, GraphHashFileKind.Binary),
            Throws.ArgumentException
        );
        Assert.That(() => new ProjectHashInput("hc", null!, "netstandard2.0"), Throws.ArgumentException);
        Assert.That(
            () => new LogicalPathRoots("/", "/", "/nuget", "/generated"),
            Throws.TypeOf<InvalidDataException>()
        );
        Assert.That(
            () => new LogicalPathRoots(@"C:\", @"c:\", @"D:\nuget", @"D:\generated"),
            Throws.TypeOf<InvalidDataException>()
        );
    }

    private static CompilationGraphHashInputs Inputs(
        string sourceText = "class C { }\n",
        string roslynText = "roslyn-v1",
        string edgeTarget = "machine",
        GraphHashFile? source = null,
        IReadOnlyList<OrderedHashValue>? arguments = null,
        IReadOnlyList<string>? profileSymbols = null
    )
    {
        GraphHashFile actualSource = source ?? File("repo:/src/C.cs", sourceText, GraphHashFileKind.Text);
        var key = new RepositoryGraphNodeKey("hc", "netstandard2.0", "base");
        return new CompilationGraphHashInputs(
            "hc-compilation-graph/v1",
            new[] { new ProjectHashInput("hc", "repo:/src/hc.csproj", "netstandard2.0") },
            new[] { new ProfileHashInput("base", profileSymbols ?? Array.Empty<string>()) },
            new[] { Node("base", actualSource, arguments) },
            new[] { new RepositoryProjectEdge("hc", edgeTarget) },
            Toolchain(roslynText),
            File("repo:/eng/HcSemanticCompilerInputs.targets", "<Project />\n", GraphHashFileKind.Text),
            new OptionalGraphHashFile(false, null)
        );
    }

    private static NodeHashInput Node(
        string profileId,
        GraphHashFile source,
        IReadOnlyList<OrderedHashValue>? arguments = null
    ) =>
        new(
            new RepositoryGraphNodeKey("hc", "netstandard2.0", profileId),
            new Dictionary<string, string> { ["nullable"] = "enable" },
            arguments ?? new[] { new OrderedHashValue(0, "/nullable:enable") },
            new[] { new OrderedGraphHashFile(0, source) },
            Array.Empty<ReferenceHashInput>(),
            Array.Empty<ProjectReferenceHashInput>(),
            Array.Empty<AnalyzerHashInput>(),
            Array.Empty<GraphHashFile>(),
            Array.Empty<GraphHashFile>(),
            Array.Empty<GraphHashFile>(),
            new[] { File("repo:/obj/project.assets.json", "{}\n", GraphHashFileKind.Json) },
            new[] { File("repo:/Directory.Build.props", "<Project />\n", GraphHashFileKind.Text) }
        );

    private static ToolchainHashInput Toolchain(string roslynText = "roslyn-v1") =>
        new(
            "10.0.303",
            "18.0.0",
            "Microsoft.CodeAnalysis.CSharp, Version=5.6.0.0",
            "csc 5.6.0",
            "hc-conformance-loader/v1",
            new[] { File("sdk:/Roslyn/Microsoft.CodeAnalysis.CSharp.dll", roslynText, GraphHashFileKind.Binary) }
        );

    private static CompilationGraphHashInputs InputsWithAsset(GraphHashFile asset) =>
        new(
            "hc-compilation-graph/v1",
            new[] { new ProjectHashInput("hc", "repo:/src/hc.csproj", "netstandard2.0") },
            new[] { new ProfileHashInput("base", Array.Empty<string>()) },
            new[]
            {
                new NodeHashInput(
                    new RepositoryGraphNodeKey("hc", "netstandard2.0", "base"),
                    new Dictionary<string, string>(),
                    Array.Empty<OrderedHashValue>(),
                    new[]
                    {
                        new OrderedGraphHashFile(0, File("repo:/src/C.cs", "class C { }\n", GraphHashFileKind.Text)),
                    },
                    Array.Empty<ReferenceHashInput>(),
                    Array.Empty<ProjectReferenceHashInput>(),
                    Array.Empty<AnalyzerHashInput>(),
                    Array.Empty<GraphHashFile>(),
                    Array.Empty<GraphHashFile>(),
                    Array.Empty<GraphHashFile>(),
                    new[] { asset },
                    Array.Empty<GraphHashFile>()
                ),
            },
            Array.Empty<RepositoryProjectEdge>(),
            Toolchain(),
            File("repo:/eng/HcSemanticCompilerInputs.targets", "<Project />\n", GraphHashFileKind.Text),
            new OptionalGraphHashFile(false, null)
        );

    private static CompilationGraphHashInputs InputsWithEditorConfig(GraphHashFile editorConfig) =>
        new(
            "hc-compilation-graph/v1",
            new[] { new ProjectHashInput("hc", "repo:/src/hc.csproj", "netstandard2.0") },
            new[] { new ProfileHashInput("base", Array.Empty<string>()) },
            new[]
            {
                new NodeHashInput(
                    new RepositoryGraphNodeKey("hc", "netstandard2.0", "base"),
                    new Dictionary<string, string>(),
                    Array.Empty<OrderedHashValue>(),
                    new[]
                    {
                        new OrderedGraphHashFile(0, File("repo:/src/C.cs", "class C { }\n", GraphHashFileKind.Text)),
                    },
                    Array.Empty<ReferenceHashInput>(),
                    Array.Empty<ProjectReferenceHashInput>(),
                    Array.Empty<AnalyzerHashInput>(),
                    Array.Empty<GraphHashFile>(),
                    new[] { editorConfig },
                    Array.Empty<GraphHashFile>(),
                    Array.Empty<GraphHashFile>(),
                    Array.Empty<GraphHashFile>()
                ),
            },
            Array.Empty<RepositoryProjectEdge>(),
            Toolchain(),
            File("repo:/eng/HcSemanticCompilerInputs.targets", "<Project />\n", GraphHashFileKind.Text),
            new OptionalGraphHashFile(false, null)
        );

    private static GraphHashFile File(string path, string content, GraphHashFileKind kind) =>
        new(path, ImmutableArray.CreateRange(Encoding.UTF8.GetBytes(content)), kind);
}
