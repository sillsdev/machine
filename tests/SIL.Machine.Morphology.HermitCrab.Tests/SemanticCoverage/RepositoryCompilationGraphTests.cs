using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class RepositoryCompilationGraphTests
{
    [Test]
    public void FixedGraphHasTheExactFourProjectsFourProfilesAndSixteenNodes()
    {
        RepositoryCompilationGraph graph = RepositoryCompilationGraph.CreateFixed();

        Assert.That(
            graph.Projects.Select(project => project.Id),
            Is.EqualTo(new[] { "machine", "hc", "hc-tool", "hc-conformance" })
        );
        Assert.That(
            graph.Projects.Select(project => project.RelativePath),
            Is.EqualTo(
                new[]
                {
                    "src/SIL.Machine/SIL.Machine.csproj",
                    "src/SIL.Machine.Morphology.HermitCrab/SIL.Machine.Morphology.HermitCrab.csproj",
                    "src/SIL.Machine.Morphology.HermitCrab.Tool/SIL.Machine.Morphology.HermitCrab.Tool.csproj",
                    "src/SIL.Machine.Morphology.HermitCrab.Conformance/SIL.Machine.Morphology.HermitCrab.Conformance.csproj",
                }
            )
        );
        Assert.That(
            graph.Projects.Select(project => project.TargetFramework),
            Is.EqualTo(new[] { "netstandard2.0", "netstandard2.0", "net10.0", "net10.0" })
        );
        Assert.That(
            graph.Projects.Select(project => project.DirectOwnedReferences),
            Is.EqualTo(new[] { Array.Empty<string>(), new[] { "machine" }, new[] { "hc" }, new[] { "hc", "hc-tool" } })
        );
        Assert.That(
            graph.Profiles.Select(profile => profile.Id),
            Is.EqualTo(new[] { "base", "single-threaded", "output-analyses", "combined" })
        );
        Assert.That(
            graph.Profiles.Select(profile => profile.AdditionalSymbols),
            Is.EqualTo(
                new[]
                {
                    Array.Empty<string>(),
                    new[] { "SINGLE_THREADED" },
                    new[] { "OUTPUT_ANALYSES" },
                    new[] { "SINGLE_THREADED", "OUTPUT_ANALYSES" },
                }
            )
        );
        Assert.That(
            graph.Profiles.Single(profile => profile.Id == "combined").AdditionalSymbols,
            Is.EqualTo(new[] { "SINGLE_THREADED", "OUTPUT_ANALYSES" })
        );
        Assert.That(graph.Nodes, Has.Count.EqualTo(16));
        Assert.That(
            graph.Nodes.Select(node => node.Key),
            Is.EqualTo(
                from project in graph.Projects
                from profile in graph.Profiles
                select new RepositoryGraphNodeKey(project.Id, project.TargetFramework, profile.Id)
            )
        );
        Assert.That(
            graph.ProjectEdges,
            Is.EqualTo(
                new[]
                {
                    new RepositoryProjectEdge("hc", "machine"),
                    new RepositoryProjectEdge("hc-tool", "hc"),
                    new RepositoryProjectEdge("hc-conformance", "hc"),
                    new RepositoryProjectEdge("hc-conformance", "hc-tool"),
                }
            )
        );
    }

    [Test]
    public void MissingAndExtraNodesAreRejected()
    {
        RepositoryCompilationGraph fixedGraph = RepositoryCompilationGraph.CreateFixed();

        List<RepositoryGraphNode> missing = fixedGraph.Nodes.Skip(1).ToList();
        Assert.That(
            () => RepositoryCompilationGraph.Create(missing, fixedGraph.ProjectEdges),
            Throws.TypeOf<InvalidDataException>()
        );

        List<RepositoryGraphNode> extra = fixedGraph.Nodes.ToList();
        extra.Add(
            new RepositoryGraphNode(
                "machine",
                "src/SIL.Machine/SIL.Machine.csproj",
                "netstandard2.0",
                fixedGraph.Profiles[0]
            )
        );
        Assert.That(
            () => RepositoryCompilationGraph.Create(extra, fixedGraph.ProjectEdges),
            Throws.TypeOf<InvalidDataException>()
        );
    }

    [Test]
    public void OutsideProjectAndCyclicEdgesAreRejected()
    {
        RepositoryCompilationGraph fixedGraph = RepositoryCompilationGraph.CreateFixed();
        List<RepositoryGraphNode> outside = fixedGraph.Nodes.ToList();
        outside[0] = new RepositoryGraphNode(
            "machine",
            "../other/SIL.Machine.csproj",
            "netstandard2.0",
            outside[0].Profile
        );
        Assert.That(
            () => RepositoryCompilationGraph.Create(outside, fixedGraph.ProjectEdges),
            Throws.TypeOf<InvalidDataException>()
        );

        List<RepositoryProjectEdge> cyclic = fixedGraph.ProjectEdges.ToList();
        cyclic.Remove(new RepositoryProjectEdge("hc", "machine"));
        cyclic.Add(new RepositoryProjectEdge("machine", "hc"));
        Assert.That(
            () => RepositoryCompilationGraph.Create(fixedGraph.Nodes, cyclic),
            Throws.TypeOf<InvalidDataException>()
        );
    }

    [Test]
    public void UnknownAndAmbiguousEdgesAreRejected()
    {
        RepositoryCompilationGraph fixedGraph = RepositoryCompilationGraph.CreateFixed();
        List<RepositoryProjectEdge> outside = fixedGraph.ProjectEdges.ToList();
        outside.RemoveAt(0);
        outside.Add(new RepositoryProjectEdge("hc", "not-owned"));
        Assert.That(
            () => RepositoryCompilationGraph.Create(fixedGraph.Nodes, outside),
            Throws.TypeOf<InvalidDataException>()
        );

        outside = fixedGraph.ProjectEdges.ToList();
        outside.Add(new RepositoryProjectEdge("hc-tool", "machine"));
        Assert.That(
            () => RepositoryCompilationGraph.Create(fixedGraph.Nodes, outside),
            Throws.TypeOf<InvalidDataException>()
        );
    }

    [Test]
    public void IndependentlyConstructedEquivalentProfileIsAcceptedAsTheSameClosedValue()
    {
        RepositoryCompilationGraph fixedGraph = RepositoryCompilationGraph.CreateFixed();
        BuildProfile equivalent = new("base", Array.Empty<string>());
        List<RepositoryGraphNode> nodes = fixedGraph.Nodes.ToList();
        nodes[0] = new RepositoryGraphNode(
            nodes[0].ProjectId,
            nodes[0].ProjectPath,
            nodes[0].TargetFramework,
            equivalent
        );

        Assert.That(RepositoryCompilationGraph.Create(nodes, fixedGraph.ProjectEdges).Nodes, Has.Count.EqualTo(16));
    }

    [Test]
    public void UnknownProfileIdAndKnownProfileWithWrongSymbolsAreRejected()
    {
        RepositoryCompilationGraph fixedGraph = RepositoryCompilationGraph.CreateFixed();
        List<RepositoryGraphNode> nodes = fixedGraph.Nodes.ToList();
        nodes[0] = new RepositoryGraphNode(
            nodes[0].ProjectId,
            nodes[0].ProjectPath,
            nodes[0].TargetFramework,
            new BuildProfile("fifth-profile", Array.Empty<string>())
        );
        Assert.That(
            () => RepositoryCompilationGraph.Create(nodes, fixedGraph.ProjectEdges),
            Throws.TypeOf<InvalidDataException>()
        );

        nodes[0] = new RepositoryGraphNode(
            nodes[0].ProjectId,
            nodes[0].ProjectPath,
            nodes[0].TargetFramework,
            new BuildProfile("base", new[] { "SINGLE_THREADED" })
        );
        Assert.That(
            () => RepositoryCompilationGraph.Create(nodes, fixedGraph.ProjectEdges),
            Throws.TypeOf<InvalidDataException>()
        );
    }

    [Test]
    public void ExplicitCompatibleTargetSelectionResolvesSyntheticMultiTargetProject()
    {
        Assert.That(
            RepositoryTargetFrameworkSelection.Select(new[] { "netstandard2.0", "net8.0" }, "netstandard2.0"),
            Is.EqualTo("netstandard2.0")
        );
        Assert.That(
            () => RepositoryTargetFrameworkSelection.Select(new[] { "netstandard2.0", "net8.0" }, null),
            Throws.TypeOf<InvalidDataException>()
        );
        Assert.That(
            () => RepositoryTargetFrameworkSelection.Select(new[] { "netstandard2.0", "net8.0" }, "net6.0"),
            Throws.TypeOf<InvalidDataException>()
        );
    }
}
