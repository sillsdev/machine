using System.Diagnostics;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class DotnetMsbuildCompilationGraphLoaderTests
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
    public void RepositoryPinsTheCompilerSdkAndRoslynPackage()
    {
        string root = RepositoryRoot();
        using JsonDocument global = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root, "global.json")));
        string project = File.ReadAllText(Path.Combine(
            root,
            "src",
            "SIL.Machine.Morphology.HermitCrab.Conformance",
            "SIL.Machine.Morphology.HermitCrab.Conformance.csproj"));
        string references = File.ReadAllText(Path.Combine(root, "eng", "HcRoslynCompilerReferences.props"));

        Assert.Multiple(() =>
        {
            Assert.That(global.RootElement.GetProperty("sdk").GetProperty("version").GetString(), Is.EqualTo("10.0.303"));
            Assert.That(global.RootElement.GetProperty("sdk").GetProperty("rollForward").GetString(), Is.EqualTo("disable"));
            Assert.That(global.RootElement.GetProperty("sdk").GetProperty("allowPrerelease").GetBoolean(), Is.False);
            Assert.That(project, Does.Not.Contain("PackageReference Include=\"Microsoft.CodeAnalysis.CSharp\""));
            Assert.That(project, Does.Contain("HcRoslynCompilerReferences.props"));
            Assert.That(references, Does.Contain("$(MSBuildToolsPath)/Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll"));
        });
    }

    [Test]
    public void CaptureTargetDeclaresTheNativeProtocolWithoutSerializationTasks()
    {
        string target = File.ReadAllText(Path.Combine(RepositoryRoot(), "eng", "HcSemanticCompilerInputs.targets"));

        Assert.Multiple(() =>
        {
            Assert.That(target, Does.Contain("_PanGlossCaptureCompilerInputs"));
            Assert.That(target, Does.Contain("PanGlossCompilerInputProtocol"));
            Assert.That(target, Does.Contain("hc-semantic-msbuild/v1"));
            Assert.That(target, Does.Contain("ResolveReferences;ResolveKeySource;SetWin32ManifestProperties;FindReferenceAssembliesForReferences;BeforeCompile;CoreCompile"));
            Assert.That(target, Does.Not.Contain("WriteLinesToFile"));
        });
    }

    [Test]
    public void ProtocolParserAcceptsTheNativeEnvelopeAndPreservesItemOrder()
    {
        byte[] json = Encoding.UTF8.GetBytes("""
            {
              "Properties": {
                "PanGlossCompilerInputProtocol": "hc-semantic-msbuild/v1",
                "MSBuildAllProjects": "repo:/Machine.sln",
                "AssemblyName": "hc",
                "TargetFramework": "netstandard2.0",
                "LangVersion": "latest",
                "Nullable": "enable",
                "DefineConstants": "TRACE;A",
                "AllowUnsafeBlocks": "false",
                "CheckForOverflowUnderflow": "false",
                "OutputType": "Library",
                "NETCoreSdkVersion": "10.0.303",
                "MSBuildVersion": "17.0.0",
                "CscToolPath": "",
                "RoslynAssembliesPath": "sdk:/Roslyn",
                "GeneratedAssemblyInfoFile": "tmp:/Project.AssemblyInfo.cs",
                "TargetFrameworkMonikerAssemblyAttributesPath": "tmp:/.NETStandard,Version=v2.0.AssemblyAttributes.cs"
              },
              "Items": {
                "Compile": [
                  { "Identity": "repo:/b.cs", "FullPath": "repo:/b.cs" },
                  { "Identity": "repo:/a.cs", "FullPath": "repo:/a.cs" }
                ],
                "CscCommandLineArgs": [],
                "ProjectReference": [],
                "ReferencePathWithRefAssemblies": [],
                "Analyzer": [],
                "AdditionalFiles": [],
                "EditorConfigFiles": [],
                "Using": []
              }
            }
            """);

        CapturedCompilerInputs captured = MsBuildCaptureProtocol.Parse(json);

        Assert.That(captured.Items["Compile"].Select(item => item.Identity), Is.EqualTo(new[] { "repo:/b.cs", "repo:/a.cs" }));
        Assert.That(captured.Items["Compile"][0].Metadata["FullPath"], Is.EqualTo("repo:/b.cs"));
    }

    [TestCase("{}")]
    [TestCase("{\"Properties\":{},\"Items\":{}}")]
    [TestCase("{\"Properties\":{\"PanGlossCompilerInputProtocol\":\"wrong\"},\"Items\":{}}")]
    [TestCase(
        "{\"Properties\":{\"PanGlossCompilerInputProtocol\":\"hc-semantic-msbuild/v1\",\"MSBuildAllProjects\":\"x\",\"AssemblyName\":\"x\",\"TargetFramework\":\"net10.0\",\"LangVersion\":\"latest\",\"Nullable\":\"enable\",\"DefineConstants\":\"\",\"AllowUnsafeBlocks\":\"false\",\"CheckForOverflowUnderflow\":\"false\",\"OutputType\":\"Exe\",\"NETCoreSdkVersion\":\"10.0.303\",\"MSBuildVersion\":\"17\",\"CscToolPath\":\"\",\"RoslynAssembliesPath\":\"sdk:/Roslyn\",\"GeneratedAssemblyInfoFile\":\"tmp:/x.AssemblyInfo.cs\",\"TargetFrameworkMonikerAssemblyAttributesPath\":\"tmp:/.NETCoreApp,Version=v10.0.AssemblyAttributes.cs\"},\"Items\":{\"CscCommandLineArgs\":[],\"Compile\":[],\"ProjectReference\":[],\"ReferencePathWithRefAssemblies\":[],\"Analyzer\":[],\"AdditionalFiles\":[],\"EditorConfigFiles\":[],\"Using\":[],\"Mystery\":[]}}")]
    public void ProtocolParserRejectsIncompleteOrUnknownEnvelopes(string json)
    {
        Assert.That(() => MsBuildCaptureProtocol.Parse(Encoding.UTF8.GetBytes(json)), Throws.InstanceOf<InvalidDataException>());
    }

    [Test]
    public void ProtocolParserRejectsDuplicateRequestedPropertiesAndNullItemIdentities()
    {
        string duplicate = "{\"Properties\":{\"PanGlossCompilerInputProtocol\":\"hc-semantic-msbuild/v1\",\"PanGlossCompilerInputProtocol\":\"hc-semantic-msbuild/v1\"},\"Items\":{}}";
        string nullIdentity = CompleteEnvelope("[{\"Identity\":null}]");

        Assert.Multiple(() =>
        {
            Assert.That(() => MsBuildCaptureProtocol.Parse(Encoding.UTF8.GetBytes(duplicate)), Throws.InstanceOf<InvalidDataException>());
            Assert.That(() => MsBuildCaptureProtocol.Parse(Encoding.UTF8.GetBytes(nullIdentity)), Throws.InstanceOf<InvalidDataException>());
        });
    }

    [TestCase("{")]
    [TestCase("{\"Properties\": null}")]
    public void ProtocolParserRejectsMalformedJson(string json)
    {
        Assert.That(() => MsBuildCaptureProtocol.Parse(Encoding.UTF8.GetBytes(json)), Throws.InstanceOf<InvalidDataException>());
    }

    [Test]
    public void ProtocolParserRejectsInvalidUtf8()
    {
        Assert.That(() => MsBuildCaptureProtocol.Parse(new byte[] { 0xFF, 0xFE }), Throws.InstanceOf<InvalidDataException>());
    }

    [Test]
    public async Task LoaderUsesBoundedShellFreeQueriesAndBuildsTheSixteenNodeMatrix()
    {
        string root = RepositoryRoot();
        var runner = new RecordingMsBuildRunner();
        var loader = new RepositoryCompilationGraphLoader(runner, hashInputBuilder: SyntheticHashInputs);

        RepositoryCompilationGraph graph = await loader.LoadAsync(new RepositoryRoot(root), CancellationToken.None);

        Assert.That(graph.Nodes, Has.Count.EqualTo(16));
        Assert.That(graph.Captures, Has.Count.EqualTo(16));
        Assert.That(runner.Calls, Has.Count.EqualTo(20), "Four base-property evaluations plus sixteen profile queries are required.");
        Assert.Multiple(() =>
        {
            Assert.That(runner.Calls.All(call => !call.StartInfo.UseShellExecute), Is.True);
            Assert.That(runner.Calls.All(call => call.StartInfo.WorkingDirectory == Path.GetFullPath(root)), Is.True);
            Assert.That(runner.Calls.All(call => call.Timeout == TimeSpan.FromSeconds(120)), Is.True);
            Assert.That(runner.Calls.All(call => call.MaxOutputBytes == 64 * 1024 * 1024), Is.True);
            Assert.That(runner.Calls.Where(call => call.StartInfo.ArgumentList.Contains("/t:_PanGlossCaptureCompilerInputs")).All(call =>
                !call.StartInfo.ArgumentList.Contains("/restore") &&
                !call.StartInfo.ArgumentList.Contains("--restore") &&
                call.StartInfo.ArgumentList.Contains("/nr:false") &&
                call.StartInfo.ArgumentList.Contains("/p:BuildProjectReferences=false") &&
                call.StartInfo.ArgumentList.Any(argument => argument.StartsWith("/p:ProjectAssetsFile=", StringComparison.Ordinal))), Is.True);
            Assert.That(runner.Calls.Where(call => call.StartInfo.ArgumentList.Contains("/t:_PanGlossCaptureCompilerInputs")).All(call =>
                call.StartInfo.ArgumentList.Any(argument => argument.StartsWith("/p:IntermediateOutputPath=", StringComparison.Ordinal))), Is.True);
            Assert.That(runner.Calls.SelectMany(call => call.StartInfo.ArgumentList).Any(argument =>
                argument == "/p:DefineConstants=OUTPUT_ANALYSES%3BSINGLE_THREADED%3BTRACE"), Is.True);
        });
    }

    private static CompilationGraphHashInputs SyntheticHashInputs(CompilationGraphHashEnvironment environment)
    {
        RepositoryCompilationGraph graph = environment.Graph;
        GraphHashFile captureTarget = HashFile("repo:/eng/HcSemanticCompilerInputs.targets", "synthetic-target");
        return new CompilationGraphHashInputs(
            "hc-compilation-graph/v1",
            graph.Projects.Select(project => new ProjectHashInput(
                project.Id,
                $"repo:/{project.RelativePath}",
                project.TargetFramework)).ToArray(),
            graph.Profiles.Select(profile => new ProfileHashInput(profile.Id, profile.AdditionalSymbols)).ToArray(),
            graph.Nodes.Select((node, ordinal) => new NodeHashInput(
                node.Key,
                new Dictionary<string, string> { ["synthetic"] = "true" },
                Array.Empty<OrderedHashValue>(),
                new[] { new OrderedGraphHashFile(0, HashFile($"generated:/{node.ProjectId}/{node.Profile.Id}/source.cs", $"// {ordinal}")) },
                Array.Empty<ReferenceHashInput>(),
                Array.Empty<ProjectReferenceHashInput>(),
                Array.Empty<AnalyzerHashInput>(),
                Array.Empty<GraphHashFile>(),
                Array.Empty<GraphHashFile>(),
                Array.Empty<GraphHashFile>(),
                Array.Empty<GraphHashFile>(),
                Array.Empty<GraphHashFile>())).ToArray(),
            graph.ProjectEdges,
            new ToolchainHashInput(
                "synthetic-sdk",
                "synthetic-msbuild",
                "synthetic-roslyn",
                "synthetic-compiler",
                "synthetic-loader",
                new[] { HashFile("sdk:/synthetic/compiler.dll", "synthetic-toolchain", GraphHashFileKind.Binary) }),
            captureTarget,
            new OptionalGraphHashFile(false, null));
    }

    private static GraphHashFile HashFile(
        string logicalPath,
        string content,
        GraphHashFileKind kind = GraphHashFileKind.Text) =>
        new(logicalPath, ImmutableArray.CreateRange(Encoding.UTF8.GetBytes(content)), kind);

    [TestCase(7, "", TestName = "LoaderRejectsNonzeroExit")]
    [TestCase(0, "warning", TestName = "LoaderRejectsStandardError")]
    public void LoaderRejectsUncleanProcessResults(int exitCode, string standardError)
    {
        var runner = new RecordingMsBuildRunner((_, _) => new ProcessCapture(
            exitCode,
            Encoding.UTF8.GetBytes("{\"Properties\":{\"DefineConstants\":\"TRACE\",\"TargetFramework\":\"netstandard2.0\"}}"),
            standardError));
        var loader = new RepositoryCompilationGraphLoader(runner);

        Assert.That(
            async () => await loader.LoadAsync(new RepositoryRoot(RepositoryRoot()), CancellationToken.None),
            Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void LoaderRejectsOversizedOutputAndProtocolMismatch()
    {
        var oversized = new RecordingMsBuildRunner((_, _) => new ProcessCapture(
            0,
            new byte[RepositoryCompilationGraphLoader.MaximumStandardOutputBytes + 1],
            ""));
        var mismatched = new RecordingMsBuildRunner((start, call) =>
        {
            if (call < 4)
            {
                return new ProcessCapture(
                    0,
                    Encoding.UTF8.GetBytes($"{{\"Properties\":{{\"DefineConstants\":\"TRACE\",\"TargetFramework\":\"{ArgumentValue(start, "/p:TargetFramework=")}\"}}}}"),
                    "");
            }
            string target = ArgumentValue(start, "/p:TargetFramework=");
            return new ProcessCapture(0, Encoding.UTF8.GetBytes(
                CompleteEnvelope("[]", target).Replace(
                    "hc-semantic-msbuild/v1",
                    "hc-semantic-msbuild/v2",
                    StringComparison.Ordinal)), "");
        });

        Assert.Multiple(() =>
        {
            Assert.That(
                async () => await new RepositoryCompilationGraphLoader(oversized).LoadAsync(
                    new RepositoryRoot(RepositoryRoot()), CancellationToken.None),
                Throws.TypeOf<InvalidDataException>());
            Assert.That(
                async () => await new RepositoryCompilationGraphLoader(mismatched).LoadAsync(
                    new RepositoryRoot(RepositoryRoot()), CancellationToken.None),
                Throws.TypeOf<InvalidDataException>());
        });
    }

    [Test]
    public void LoaderPropagatesTimeoutAndTerminationFailures()
    {
        var timeout = new RecordingMsBuildRunner((_, _) => throw new InvalidDataException("timeout"));
        var termination = new RecordingMsBuildRunner((_, _) => throw new InvalidDataException("termination unconfirmed"));

        Assert.Multiple(() =>
        {
            Assert.That(
                async () => await new RepositoryCompilationGraphLoader(timeout).LoadAsync(
                    new RepositoryRoot(RepositoryRoot()), CancellationToken.None),
                Throws.TypeOf<InvalidDataException>());
            Assert.That(
                async () => await new RepositoryCompilationGraphLoader(termination).LoadAsync(
                    new RepositoryRoot(RepositoryRoot()), CancellationToken.None),
                Throws.TypeOf<InvalidDataException>());
        });
    }

    [Test]
    public void LoaderFailsBeforeProcessExecutionWhenRestoredAssetsAreMissing()
    {
        string scratch = Path.Combine(Path.GetTempPath(), "hc-missing-assets", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(scratch, "eng"));
            Directory.CreateDirectory(Path.Combine(scratch, "src", "SIL.Machine"));
            File.WriteAllText(Path.Combine(scratch, "eng", "HcSemanticCompilerInputs.targets"), "<Project />");
            File.WriteAllText(
                Path.Combine(scratch, "src", "SIL.Machine", "SIL.Machine.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            var runner = new RecordingMsBuildRunner();

            Assert.That(
                async () => await new RepositoryCompilationGraphLoader(runner).LoadAsync(
                    new RepositoryRoot(scratch), CancellationToken.None),
                Throws.TypeOf<DirectoryNotFoundException>());
            Assert.That(runner.Calls, Is.Empty);
        }
        finally
        {
            if (Directory.Exists(scratch))
                Directory.Delete(scratch, recursive: true);
        }
    }

    [Test]
    public void ProcessRunnerEnforcesTheOutputLimitWithoutUsingAShell()
    {
        var start = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add("--info");

        Assert.That(
            async () => await new MsBuildProcessRunner().RunAsync(
                start,
                TimeSpan.FromSeconds(30),
                maxStandardOutputBytes: 1,
                CancellationToken.None),
            Throws.TypeOf<InvalidDataException>());
    }

    // cancellationToken must be threaded all the way into LoadAsync: MsBuildProcessRunner already kills
    // its child's whole process tree on cancellation (see TryTerminate), but only for the token it is
    // actually given -- passing CancellationToken.None here would silently disconnect [CancelAfter] from
    // that kill path and leave a live `dotnet msbuild` running past a cancelled or timed-out test.
    [Test]
    [CancelAfter(120_000)]
    public async Task LiveLoaderCapturesEveryRestoredNodeWithoutRepositoryWrites(CancellationToken cancellationToken)
    {
        string root = RepositoryRoot();
        IReadOnlyDictionary<string, FileStamp> before = RepositoryFileStamps(root);

        RepositoryCompilationGraph graph = await new RepositoryCompilationGraphLoader(
            new MsBuildProcessRunner()).LoadAsync(new RepositoryRoot(root), cancellationToken);

        CapturedCompilerInputs firstCapture = graph.Captures.OrderBy(pair => pair.Key.ProjectId, StringComparer.Ordinal).First().Value;
        string compatibilitySource = Path.Combine(root, "src", "SIL.Machine", "Utils", "StringExtensions.cs");
        Assert.That(File.Exists(compatibilitySource), Is.True);
        Assert.DoesNotThrow(() => CSharpCommandLineInputParser.Parse(
            new[] { "/out:compatibility.dll", compatibilitySource },
            Path.GetDirectoryName(compatibilitySource)!,
            queriedToolchain: new CompilerToolchainIdentity(firstCapture.Properties["RoslynAssembliesPath"])));

        IReadOnlyDictionary<string, FileStamp> after = RepositoryFileStamps(root);
        Assert.Multiple(() =>
        {
            Assert.That(graph.Captures, Has.Count.EqualTo(16));
            Assert.That(graph.CompilerInputs, Has.Count.EqualTo(16));
            Assert.That(graph.CompilerInputs.All(pair => pair.Value.Sources.Count > 0), Is.True);
            Assert.That(graph.CompilerInputs.All(pair => pair.Value.Sources.All(source => source.Content.Length > 0)), Is.True);
            Assert.That(graph.CompilerInputs.All(pair => pair.Value.AnalyzerConfigs.Count > 0), Is.True);
            Assert.That(graph.CompilerInputs.All(pair => pair.Value.AnalyzerConfigs.All(config => config.Content.Length > 0)), Is.True);
            Assert.That(graph.CompilerInputs.All(pair => pair.Value.AnalyzerConfigs.Any(config => !File.Exists(config.Path))), Is.True,
                "Each node must retain its private generated analyzer configuration after capture cleanup.");
            Assert.That(graph.CompilerInputs.Where(pair => pair.Key.TargetFramework == "net10.0").All(pair =>
                pair.Value.Analyzers.Count(analyzer => analyzer.Disposition == AnalyzerDisposition.SdkOwnedSourceGeneratorPendingProbe) == 5), Is.True);
            Assert.That(graph.CompilerInputs.Where(pair => pair.Key.TargetFramework == "netstandard2.0").All(pair =>
                pair.Value.Analyzers.All(analyzer => analyzer.Disposition == AnalyzerDisposition.Ordinary)), Is.True);
            Assert.That(graph.Captures.All(pair =>
                pair.Value.Properties["PanGlossCompilerInputProtocol"] == MsBuildCaptureProtocol.Version), Is.True);
            Assert.That(graph.Captures.All(pair =>
                pair.Value.Properties["TargetFramework"] == pair.Key.TargetFramework), Is.True);
            Assert.That(graph.Captures.All(pair => pair.Value.Items["CscCommandLineArgs"].Count > 0), Is.True);
            Assert.That(graph.HashInputs.SchemaVersion, Is.EqualTo("hc-compilation-graph/v1"));
            Assert.That(graph.HashInputs.Nodes, Has.Length.EqualTo(16));
            Assert.That(graph.HashInputs.Nodes.All(node => node.Sources.Length > 0), Is.True);
            Assert.That(graph.HashInputs.Nodes.All(node => node.Assets.Length >= 3), Is.True,
                "Assets JSON and generated NuGet props/targets must be retained before hashing.");
            Assert.That(graph.HashInputs.Nodes.All(node => node.Imports.Length > 0), Is.True);
            Assert.That(graph.HashInputs.Nodes.Where(node => node.Key.ProjectId != "machine")
                .All(node => node.ProjectReferences.Length > 0), Is.True);
            Assert.That(graph.HashInputs.Nodes.All(node => node.References.Length > 0), Is.True);
            Assert.That(graph.HashInputs.Toolchain.Files, Is.Not.Empty);
            Assert.That(graph.HashInputs.Toolchain.LoaderIdentity, Is.Not.Empty);
            Assert.That(graph.Hashes, Is.EqualTo(CompilationGraphHashing.Compute(graph.HashInputs)));
            Assert.That(new[] { graph.Hashes.GraphInputHash, graph.Hashes.ToolchainHash, graph.Hashes.GraphHash }
                .All(hash => hash.Length == 64 && hash.All(character => char.IsAsciiHexDigitLower(character))), Is.True);
            Assert.That(CanonicalJson.Serialize(graph.HashInputs), Does.Not.Contain(Path.GetFullPath(root)));
            Assert.That(after, Is.EqualTo(before), "Compiler-input capture must not write inside the repository.");
        });
    }

    [Test]
    public void CommandLineParserPreservesRoslynOptionsReferencesAndAuxiliaryInputs()
    {
        string root = RepositoryRoot();
        string source = Path.Combine(root, "src", "SIL.Machine", "Utils", "StringExtensions.cs");
        string additionalFile = Path.Combine(root, "conformance", "constructs.txt");
        string reference = typeof(object).Assembly.Location;
        string analyzer = typeof(CSharpCompilation).Assembly.Location;
        var args = new[]
        {
            $"/define:TRACE;BASE;SINGLE_THREADED;OUTPUT_ANALYSES", "/langversion:preview", "/nullable:enable",
            "/unsafe+", "/checked+", "/reference:Alias=" + reference,
            "/link:" + reference, "/analyzer:" + analyzer, "/additionalfile:" + additionalFile,
            "/analyzerconfig:.editorconfig", "/out:out.dll", source,
        };

        CompilerInputModel model = CSharpCommandLineInputParser.Parse(
            args,
            root,
            new[] { "OUTPUT_ANALYSES" },
            new CompilerToolchainIdentity(Path.GetDirectoryName(typeof(CSharpCommandLineParser).Assembly.Location)!));

        Assert.Multiple(() =>
        {
            Assert.That(((CSharpParseOptions)model.Arguments.ParseOptions).LanguageVersion, Is.EqualTo(LanguageVersion.Preview));
            Assert.That(((CSharpCompilationOptions)model.Arguments.CompilationOptions).NullableContextOptions, Is.EqualTo(NullableContextOptions.Enable));
            Assert.That(((CSharpCompilationOptions)model.Arguments.CompilationOptions).AllowUnsafe, Is.True);
            Assert.That(model.Arguments.CompilationOptions.CheckOverflow, Is.True);
            Assert.That(model.Arguments.MetadataReferences, Has.Length.EqualTo(2));
            Assert.That(model.Arguments.MetadataReferences[0].Properties.Aliases, Does.Contain("Alias"));
            Assert.That(model.Arguments.MetadataReferences.Single(item => item.Properties.EmbedInteropTypes).Properties.EmbedInteropTypes, Is.True);
            Assert.That(model.Arguments.SourceFiles.Select(file => file.Path), Is.EqualTo(new[] { source }));
            Assert.That(model.Arguments.AnalyzerReferences, Has.Length.EqualTo(1));
            Assert.That(model.Arguments.AdditionalFiles.Select(file => file.Path), Is.EqualTo(new[] { additionalFile }));
            Assert.That(model.Arguments.AnalyzerConfigPaths, Is.EqualTo(new[] { Path.Combine(root, ".editorconfig") }));
            Assert.That(model.AdditionalFiles.Single().Path, Is.EqualTo(additionalFile));
            Assert.That(model.AdditionalFiles.Single().Content, Is.Not.Empty);
            Assert.That(model.AnalyzerConfigs.Single().Path, Is.EqualTo(Path.Combine(root, ".editorconfig")));
            Assert.That(model.AnalyzerConfigs.Single().Content, Is.Not.Empty);
            Assert.That(model.Symbols, Is.EqualTo(new[] { "BASE", "OUTPUT_ANALYSES", "SINGLE_THREADED", "TRACE" }));
            Assert.That(((CSharpParseOptions)model.Arguments.ParseOptions).PreprocessorSymbolNames,
                Is.SupersetOf(new[] { "OUTPUT_ANALYSES", "SINGLE_THREADED" }));
        });
    }

    [TestCase("/unknown-switch", "unknown-compiler-switch")]
    [TestCase("/reference:a,b=missing.dll", "reference-parser-diagnostic")]
    public void CommandLineParserFailsClosedOnDiagnostics(string argument, string code)
    {
        CompilerInputException exception = Assert.Throws<CompilerInputException>(() =>
            CSharpCommandLineInputParser.Parse(new[] { argument }, RepositoryRoot()))!;

        Assert.That(exception.Code, Is.EqualTo(code), exception.Message);
    }

    [Test]
    public void CommandLineParserRejectsProfileSymbolsMissingFromFinalParseOptions()
    {
        CompilerInputException exception = Assert.Throws<CompilerInputException>(() =>
            CSharpCommandLineInputParser.Parse(
                new[] { "/define:TRACE", "/out:test.dll", Path.Combine(RepositoryRoot(), "src", "SIL.Machine", "Utils", "StringExtensions.cs") },
                RepositoryRoot(),
                new[] { "OUTPUT_ANALYSES" }))!;

        Assert.That(exception.Code, Is.EqualTo("compiler-profile-symbol-mismatch"));
    }

    [Test]
    public void SourceClassifierAdmitsOnlyOwnedAndKnownSdkGeneratedSupport()
    {
        string root = RepositoryRoot();
        string project = Path.Combine(root, "src", "Project");
        string intermediate = Path.Combine(project, "obj", "Debug", "net10.0");
        string assemblyInfo = Path.Combine(intermediate, "Project.AssemblyInfo.cs");
        string net10Attributes = Path.Combine(intermediate, ".NETCoreApp,Version=v10.0.AssemblyAttributes.cs");
        string netstandardAttributes = Path.Combine(intermediate, ".NETStandard,Version=v2.0.AssemblyAttributes.cs");
        var classifier = new CompilerSourceClassifier(root, project, intermediate, assemblyInfo, net10Attributes);
        var netstandardClassifier = new CompilerSourceClassifier(root, project, intermediate, assemblyInfo, netstandardAttributes);

        Assert.Multiple(() =>
        {
            Assert.That(classifier.Classify(Path.Combine(project, "Owned.cs")), Is.EqualTo(CompilerSourceKind.Owned));
            Assert.That(classifier.Classify(assemblyInfo), Is.EqualTo(CompilerSourceKind.GeneratedSupport));
            Assert.That(classifier.Classify(net10Attributes), Is.EqualTo(CompilerSourceKind.GeneratedSupport));
            Assert.That(netstandardClassifier.Classify(netstandardAttributes), Is.EqualTo(CompilerSourceKind.GeneratedSupport));
            Assert.That(() => classifier.Classify(Path.Combine(intermediate, "Evil.AssemblyInfo.cs")), Throws.TypeOf<CompilerInputException>().With.Property("Code").EqualTo("unsupported-compiler-source"));
            Assert.That(() => classifier.Classify(Path.Combine(intermediate, ".Spoof,Version=v999.AssemblyAttributes.cs")), Throws.TypeOf<CompilerInputException>().With.Property("Code").EqualTo("unsupported-compiler-source"));
            Assert.That(() => classifier.Classify(Path.Combine(intermediate, "Custom.Generated.cs")), Throws.TypeOf<CompilerInputException>().With.Property("Code").EqualTo("unsupported-compiler-source"));
            Assert.That(() => classifier.Classify(Path.Combine(project, "Generated.cs"), new Dictionary<string, string> { ["AutoGen"] = "true" }), Throws.TypeOf<CompilerInputException>().With.Property("Code").EqualTo("unsupported-compiler-source"));
            Assert.That(() => classifier.Classify(Path.Combine(project, "obj", "Generated.cs")), Throws.TypeOf<CompilerInputException>().With.Property("Code").EqualTo("unsupported-compiler-source"));
            Assert.That(() => classifier.Classify(Path.Combine(project, "GlobalUsings.g.cs")), Throws.TypeOf<CompilerInputException>().With.Property("Code").EqualTo("unsupported-implicit-global-using"));
            Assert.That(() => classifier.Classify(Path.Combine(root, "outside.cs")), Throws.TypeOf<CompilerInputException>().With.Property("Code").EqualTo("unsupported-compiler-source"));
        });
    }

    [Test]
    public void AnalyzerInspectorRecordsProvenanceAndRejectsUnownedSourceGenerators()
    {
        string path = typeof(CSharpCompilation).Assembly.Location;
        AnalyzerMetadataInspection inspection = AnalyzerMetadataInspector.Inspect(path);
        Assert.That(inspection.IsSourceGenerator, Is.False);
        Assert.That(inspection.LoadedAssembly, Is.False);
        Assert.That(inspection.Disposition, Is.EqualTo(AnalyzerDisposition.Ordinary));
        Assert.That(inspection.AssemblyIdentity, Does.Contain("Microsoft.CodeAnalysis"));
        Assert.That(inspection.Sha256, Has.Length.EqualTo(64));
        Assert.That(inspection.ReferencePackVersion, Is.Null);

        string generatorPath = EmitSyntheticGenerator();
        try
        {
            AnalyzerMetadataInspection generator = AnalyzerMetadataInspector.Inspect(generatorPath);
            Assert.That(generator.IsSourceGenerator, Is.True);
            Assert.That(generator.LoadedAssembly, Is.False);
            Assert.That(generator.Disposition, Is.EqualTo(AnalyzerDisposition.Ordinary));
            Assert.That(generator.ReferencePackVersion, Is.Null);
            Assert.That(generator.Sha256, Has.Length.EqualTo(64));

            CompilerInputException parserException = Assert.Throws<CompilerInputException>(() =>
                CSharpCommandLineInputParser.Parse(
                    new[] { $"/analyzer:{generatorPath}", "/out:test.dll", Path.Combine(RepositoryRoot(), "src", "SIL.Machine", "Utils", "StringExtensions.cs") },
                    RepositoryRoot()))!;
            Assert.That(parserException.Code, Is.EqualTo("unsupported-source-generator"));

            string spoofPath = Path.Combine(
                Path.GetTempPath(),
                "packs",
                "Microsoft.NETCore.App.Ref",
                "10.0.11",
                "analyzers",
                "dotnet",
                "cs-spoof",
                "Microsoft.Interop.LibraryImportGenerator.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(spoofPath)!);
            try
            {
                File.Copy(generatorPath, spoofPath, overwrite: true);
                AnalyzerMetadataInspection spoof = AnalyzerMetadataInspector.Inspect(spoofPath);
                Assert.That(spoof.IsSourceGenerator, Is.True);
                Assert.That(spoof.Disposition, Is.EqualTo(AnalyzerDisposition.Ordinary));
                Assert.That(spoof.ReferencePackVersion, Is.Null);
                Assert.That(() => CSharpCommandLineInputParser.Parse(
                        new[] { $"/analyzer:{spoofPath}", "/out:test.dll", Path.Combine(RepositoryRoot(), "src", "SIL.Machine", "Utils", "StringExtensions.cs") },
                        RepositoryRoot()),
                    Throws.TypeOf<CompilerInputException>().With.Property("Code").EqualTo("unsupported-source-generator"));
            }
            finally
            {
                if (File.Exists(spoofPath))
                    File.Delete(spoofPath);
            }

            string admittedDirectory = Path.Combine(
                Path.GetTempPath(), "admitted-sdk", "packs", "Microsoft.NETCore.App.Ref", "10.0.11", "analyzers", "dotnet", "cs");
            string admittedPath = Path.Combine(admittedDirectory, "Microsoft.Interop.LibraryImportGenerator.dll");
            string admittedGenerator = EmitSyntheticGenerator("Microsoft.Interop.LibraryImportGenerator");
            Directory.CreateDirectory(admittedDirectory);
            try
            {
                File.Copy(admittedGenerator, admittedPath, overwrite: true);
                AnalyzerMetadataInspection admitted = AnalyzerMetadataInspector.Inspect(admittedPath, new[] { admittedDirectory });
                Assert.That(admitted.Disposition, Is.EqualTo(AnalyzerDisposition.SdkOwnedSourceGeneratorPendingProbe));
                Assert.That(admitted.ReferencePackVersion, Is.EqualTo("10.0.11"));
                Assert.That(admitted.Sha256, Does.Match("^[0-9a-f]{64}$"));
            }
            finally
            {
                File.Delete(admittedGenerator);
                if (File.Exists(admittedPath))
                    File.Delete(admittedPath);
            }

            string helperPath = Path.Combine(
                Path.GetTempPath(),
                "packs",
                "Microsoft.NETCore.App.Ref",
                "10.0.11",
                "analyzers",
                "dotnet",
                "cs",
                "Microsoft.Interop.SourceGeneration.dll");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(helperPath)!);
                File.Copy(path, helperPath, overwrite: true);
                AnalyzerMetadataInspection helper = AnalyzerMetadataInspector.Inspect(helperPath);
                Assert.That(helper.IsSourceGenerator, Is.False);
                Assert.That(helper.Disposition, Is.EqualTo(AnalyzerDisposition.Ordinary));
                Assert.That(helper.ReferencePackVersion, Is.EqualTo("10.0.11"));
            }
            finally
            {
                if (File.Exists(helperPath))
                    File.Delete(helperPath);
            }
        }
        finally
        {
            File.Delete(generatorPath);
        }

        string inheritedGeneratorPath = EmitInheritedRegisteredGenerator();
        try
        {
            AnalyzerMetadataInspection inherited = AnalyzerMetadataInspector.Inspect(inheritedGeneratorPath);
            Assert.That(inherited.IsSourceGenerator, Is.True);
            Assert.That(() => CSharpCommandLineInputParser.Parse(
                    new[] { $"/analyzer:{inheritedGeneratorPath}", "/out:test.dll", Path.Combine(RepositoryRoot(), "src", "SIL.Machine", "Utils", "StringExtensions.cs") },
                    RepositoryRoot()),
                Throws.TypeOf<CompilerInputException>().With.Property("Code").EqualTo("unsupported-source-generator"));
        }
        finally
        {
            File.Delete(inheritedGeneratorPath);
        }

        Assert.That(() => AnalyzerMetadataInspector.Inspect(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll")),
            Throws.TypeOf<CompilerInputException>().With.Property("Code").EqualTo("analyzer-metadata-diagnostic"));
    }

    [Test]
    public void CompilerIdentityMismatchIsReportedBeforeParsing()
    {
        string parser = typeof(CSharpCommandLineParser).Assembly.Location;
        string other = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        CompilerInputException exception = Assert.Throws<CompilerInputException>(() =>
            CSharpCommandLineInputParser.Parse(new[] { "/unknown-switch" }, RepositoryRoot(), Array.Empty<string>(), new CompilerToolchainIdentity(other)))!;

        Assert.That(exception.Code, Is.EqualTo("incompatible-compiler-toolchain"));
        Assert.That(exception.Message, Does.Contain(parser));
    }

    [Test]
    public void CompilerIdentityRequiresAnAbsoluteCompilerDirectory()
    {
        CompilerInputException exception = Assert.Throws<CompilerInputException>(() =>
            CSharpCommandLineInputParser.Parse(Array.Empty<string>(), RepositoryRoot(), queriedToolchain: new CompilerToolchainIdentity("relative")))!;

        Assert.That(exception.Code, Is.EqualTo("incompatible-compiler-toolchain"));
    }

    [Test]
    public void PrivateGeneratedSourceRejectsAFileSymlink()
    {
        string privateRoot = Path.Combine(Path.GetTempPath(), "hc-generated-source-test", Guid.NewGuid().ToString("N"));
        string intermediate = Path.Combine(privateRoot, "node");
        string external = Path.Combine(Path.GetTempPath(), $"hc-generated-external-{Guid.NewGuid():N}.cs");
        string link = Path.Combine(intermediate, "Project.AssemblyInfo.cs");
        Directory.CreateDirectory(intermediate);
        File.WriteAllText(external, "// external");
        try
        {
            try
            {
                File.CreateSymbolicLink(link, external);
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
            {
                Assert.Ignore($"This host cannot create a file symlink: {exception.Message}");
            }

            Assert.That(
                () => RepositoryCompilationGraphLoader.ValidatePrivateGeneratedFile(privateRoot, intermediate, link),
                Throws.TypeOf<InvalidDataException>());
        }
        finally
        {
            if (File.Exists(link))
                File.Delete(link);
            if (File.Exists(external))
                File.Delete(external);
            if (Directory.Exists(intermediate))
                Directory.Delete(intermediate);
            if (Directory.Exists(privateRoot))
                Directory.Delete(privateRoot);
        }
    }

    private sealed class RecordingMsBuildRunner : IMsBuildProcessRunner
    {
        private readonly Func<ProcessStartInfo, int, ProcessCapture>? _result;

        internal RecordingMsBuildRunner(Func<ProcessStartInfo, int, ProcessCapture>? result = null)
        {
            _result = result;
        }

        internal List<MsBuildCall> Calls { get; } = new();

        public ValueTask<ProcessCapture> RunAsync(
            ProcessStartInfo startInfo,
            TimeSpan timeout,
            int maxStandardOutputBytes,
            CancellationToken cancellationToken)
        {
            Calls.Add(new MsBuildCall(startInfo, timeout, maxStandardOutputBytes));
            if (_result is not null)
                return ValueTask.FromResult(_result(startInfo, Calls.Count - 1));
            if (startInfo.ArgumentList.Any(argument => argument.StartsWith("-getProperty:DefineConstants", StringComparison.Ordinal)))
            {
                string evaluatedTarget = ArgumentValue(startInfo, "/p:TargetFramework=");
                return ValueTask.FromResult(new ProcessCapture(
                    0,
                    Encoding.UTF8.GetBytes($"{{\"Properties\":{{\"DefineConstants\":\"TRACE\",\"TargetFramework\":\"{evaluatedTarget}\"}}}}"),
                    ""));
            }

            string target = ArgumentValue(startInfo, "/p:TargetFramework=");
            string roslynDirectory = Path.GetDirectoryName(typeof(CSharpCommandLineParser).Assembly.Location)!;
            string projectFile = startInfo.ArgumentList.Single(argument => argument.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
            string projectDirectory = Path.GetDirectoryName(projectFile)!;
            string source = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                .First(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
                    !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
            string compileItems = JsonSerializer.Serialize(new[] { new { Identity = source, FullPath = source } });
            string commandLineItems = JsonSerializer.Serialize(new[]
            {
                new { Identity = "/define:" + ArgumentValue(startInfo, "/p:DefineConstants=").Replace("%3B", ";", StringComparison.Ordinal) },
                new { Identity = "/out:test.dll" },
                new { Identity = source },
            });
            string intermediate = ArgumentValue(startInfo, "/p:IntermediateOutputPath=");
            string generatedAssemblyInfo = Path.Combine(intermediate, $"{Path.GetFileNameWithoutExtension(projectFile)}.AssemblyInfo.cs");
            string frameworkAttributes = Path.Combine(
                intermediate,
                target == "net10.0" ? ".NETCoreApp,Version=v10.0.AssemblyAttributes.cs" : ".NETStandard,Version=v2.0.AssemblyAttributes.cs");
            File.WriteAllText(generatedAssemblyInfo, "// generated assembly info");
            File.WriteAllText(frameworkAttributes, "// generated framework attributes");
            return ValueTask.FromResult(new ProcessCapture(
                0,
                Encoding.UTF8.GetBytes(CompleteEnvelope(
                    compileItems,
                    target,
                    roslynDirectory,
                    commandLineItems,
                    generatedAssemblyInfo,
                    frameworkAttributes)),
                ""));
        }
    }

    private static string EmitSyntheticGenerator(string assemblyName = "SyntheticGenerator")
    {
        const string Source = """
            using Microsoft.CodeAnalysis;
            [Generator]
            public sealed class SyntheticGenerator : ISourceGenerator
            {
                public void Initialize(GeneratorInitializationContext context) { }
                public void Execute(GeneratorExecutionContext context) { }
            }
            """;
        string path = Path.Combine(Path.GetTempPath(), $"synthetic-generator-{Guid.NewGuid():N}.dll");
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { CSharpSyntaxTree.ParseText(Source) },
            CSharpCompilationProfile.Create().CreateMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using FileStream output = File.Create(path);
        var result = compilation.Emit(output);
        Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics));
        return path;
    }

    private static string EmitInheritedRegisteredGenerator()
    {
        const string Source = """
            using Microsoft.CodeAnalysis;
            public abstract class GeneratorBase : ISourceGenerator
            {
                public void Initialize(GeneratorInitializationContext context) { }
                public void Execute(GeneratorExecutionContext context) { }
            }
            [Generator]
            public sealed class InheritedGenerator : GeneratorBase { }
            """;
        string path = Path.Combine(Path.GetTempPath(), $"inherited-generator-{Guid.NewGuid():N}.dll");
        CSharpCompilation compilation = CSharpCompilation.Create(
            "InheritedGenerator",
            new[] { CSharpSyntaxTree.ParseText(Source) },
            CSharpCompilationProfile.Create().CreateMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using FileStream output = File.Create(path);
        var result = compilation.Emit(output);
        Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics));
        return path;
    }

    private sealed record MsBuildCall(ProcessStartInfo StartInfo, TimeSpan Timeout, int MaxOutputBytes);

    private sealed record FileStamp(long Length, DateTime LastWriteTimeUtc);

    private static IReadOnlyDictionary<string, FileStamp> RepositoryFileStamps(string root)
    {
        string[] projectRoots =
        {
            "src/SIL.Machine",
            "src/SIL.Machine.Morphology.HermitCrab",
            "src/SIL.Machine.Morphology.HermitCrab.Tool",
            "src/SIL.Machine.Morphology.HermitCrab.Conformance",
        };
        return projectRoots
            .SelectMany(relative => Directory.EnumerateFiles(Path.Combine(root, relative), "*", SearchOption.AllDirectories))
            .ToDictionary(
                path => Path.GetRelativePath(root, path).Replace('\\', '/'),
                path =>
                {
                    var info = new FileInfo(path);
                    return new FileStamp(info.Length, info.LastWriteTimeUtc);
                },
                StringComparer.Ordinal);
    }

    private static string ArgumentValue(ProcessStartInfo startInfo, string prefix) =>
        startInfo.ArgumentList.Single(argument => argument.StartsWith(prefix, StringComparison.Ordinal))[prefix.Length..];

    private static string CompleteEnvelope(
        string compileItems,
        string targetFramework = "netstandard2.0",
        string? roslynAssembliesPath = null,
        string cscCommandLineArgs = "[]",
        string? generatedAssemblyInfoFile = null,
        string? targetFrameworkAttributesPath = null)
    {
        return $$"""
            {
              "Properties": {
                "PanGlossCompilerInputProtocol": "hc-semantic-msbuild/v1",
                "MSBuildAllProjects": "repo:/Machine.sln",
                "AssemblyName": "hc",
                "TargetFramework": "{{targetFramework}}",
                "LangVersion": "latest",
                "Nullable": "enable",
                "DefineConstants": "TRACE;A",
                "AllowUnsafeBlocks": "false",
                "CheckForOverflowUnderflow": "false",
                "OutputType": "Library",
                "NETCoreSdkVersion": "10.0.303",
                "MSBuildVersion": "17.0.0",
                "CscToolPath": "",
                "RoslynAssembliesPath": {{JsonSerializer.Serialize(roslynAssembliesPath ?? "sdk:/Roslyn")}},
                "GeneratedAssemblyInfoFile": {{JsonSerializer.Serialize(generatedAssemblyInfoFile ?? "tmp:/Project.AssemblyInfo.cs")}},
                "TargetFrameworkMonikerAssemblyAttributesPath": {{JsonSerializer.Serialize(targetFrameworkAttributesPath ?? "tmp:/.NETStandard,Version=v2.0.AssemblyAttributes.cs")}}
              },
              "Items": {
                "CscCommandLineArgs": {{cscCommandLineArgs}},
                "Compile": {{compileItems}},
                "ProjectReference": [],
                "ReferencePathWithRefAssemblies": [],
                "Analyzer": [],
                "AdditionalFiles": [],
                "EditorConfigFiles": [],
                "Using": []
              }
            }
            """;
    }
}
