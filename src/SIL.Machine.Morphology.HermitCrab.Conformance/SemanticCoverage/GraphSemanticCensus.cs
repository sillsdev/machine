#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>The semantic inventory of the compilations the pinned compiler actually produced.</summary>
/// <remarks>Replaces the pre-cutover pairing of a repository directory scan with
/// <see cref="CSharpCompilationProfile"/>'s runtime reference approximation. Sources, symbols,
/// options and references all come from captured compiler inputs, so nothing here is inferred from
/// the process that happens to be running the census.</remarks>
public static class GraphSemanticCensus
{
    private static readonly string[] CensusedProjects = { "hc", "hc-tool" };

    public static SemanticInventory Read(
        string repositoryRoot,
        IReadOnlyList<string> auditedScopes,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(auditedScopes);
        // Capturing compiler inputs costs an MSBuild pass, so an unusable scope is refused first.
        foreach (string scope in auditedScopes)
        {
            if (ScopeValidation.HasPattern(scope))
            {
                throw new ArgumentException(
                    $"Audited source scope '{scope}' must be exact; patterns are not allowed.",
                    nameof(auditedScopes)
                );
            }
        }

        RepositoryCompilationGraph captured = new RepositoryCompilationGraphLoader(new MsBuildProcessRunner())
            .LoadAsync(new RepositoryRoot(repositoryRoot), cancellationToken)
            .AsTask()
            .GetAwaiter()
            .GetResult();
        RoslynCompilationGraph graph = RoslynCompilationGraph.Build(captured);
        // The semantic coverage program audits the grammar format, not the C# engine's
        // internals; an empty scope list names nothing to census rather than "census
        // everything", so the C# reader (which requires at least one exact scope) is skipped
        // and the composed inventory is the DTD's surfaces alone.
        SemanticInventory csharp =
            auditedScopes.Count == 0
                ? new SemanticInventory(string.Empty, string.Empty, Array.Empty<InventorySurface>())
                : CSharpInventoryReader.ReadFromGraph(graph, captured, repositoryRoot, CensusedProjects, auditedScopes);

        string dtdPath = GrammarCoverageGate.DtdRelativePath;
        string dtdText = File.ReadAllText(
            Path.Combine(repositoryRoot, dtdPath.Replace('/', Path.DirectorySeparatorChar))
        );
        return SemanticCoverageInventory.Compose(dtdPath, dtdText, csharp, captured.Hashes.GraphHash);
    }
}
