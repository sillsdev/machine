#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

public sealed record ImpossibilityProof(string SurfaceId, string Kind, string Evidence);

/// <summary>
/// The claims that evidence for a surface is impossible rather than merely missing. A surface whose
/// counterfactual verdict is not evidence fails the gate unless it is claimed here, so the file is the
/// only way a non-evidenced surface can pass, and every kind names a check rather than an argument.
/// </summary>
public static class ImpossibilityProofs
{
    public const string RelativePath = "conformance/semantic-coverage-proofs.tsv";

    /// <summary>Value equals its attribute's DTD default, so the parser supplies it either way.</summary>
    public const string DtdDefault = "dtd-default";

    /// <summary>The engine contains no reference to the owning element.</summary>
    public const string NoConsumer = "no-consumer";

    /// <summary>The surface's data is carried by the model but never reaches a control-flow decision or
    /// the comparison signature.</summary>
    public const string NotInSignature = "not-in-signature";

    /// <summary>An engine defect prevents any word from exercising it; must name fixture and issue.</summary>
    public const string BlockedByDefect = "blocked-by-defect";

    private static readonly HashSet<string> Kinds = new(StringComparer.Ordinal)
    {
        DtdDefault,
        NoConsumer,
        NotInSignature,
        BlockedByDefect,
    };

    public static IReadOnlyList<ImpossibilityProof> Read(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        string path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        var proofs = new List<ImpossibilityProof>();
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            string[] fields = line.Split('\t');
            if (fields[0] == "surface")
            {
                continue;
            }

            if (fields.Length != 3)
            {
                throw new FormatException($"{RelativePath}: '{line}' must be surface, kind and evidence");
            }

            if (!Kinds.Contains(fields[1]))
            {
                throw new FormatException($"{RelativePath}: unknown proof kind '{fields[1]}' for '{fields[0]}'");
            }

            if (fields[2].Trim().Length == 0)
            {
                throw new FormatException($"{RelativePath}: '{fields[0]}' claims {fields[1]} with no evidence");
            }

            proofs.Add(new ImpossibilityProof(fields[0], fields[1], fields[2]));
        }

        return proofs.OrderBy(proof => proof.SurfaceId, StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    /// Surfaces whose verdict is not evidence and that no proof claims. Each is a real gap: neither a
    /// recorded delta nor a claim that one is impossible.
    /// </summary>
    public static IReadOnlyList<CounterfactualResult> Unaccounted(
        IReadOnlyList<CounterfactualResult> verdicts,
        IReadOnlyList<ImpossibilityProof> proofs
    )
    {
        ArgumentNullException.ThrowIfNull(verdicts);
        ArgumentNullException.ThrowIfNull(proofs);
        var claimed = proofs.Select(proof => proof.SurfaceId).ToHashSet(StringComparer.Ordinal);
        return verdicts
            .Where(verdict =>
                verdict.Verdict
                    is not (
                        CounterfactualVerdict.Evidenced
                        or CounterfactualVerdict.RequiredByDtd
                        or CounterfactualVerdict.RequiredByLoader
                        or CounterfactualVerdict.EvidencedJointly
                    )
            )
            .Where(verdict => !claimed.Contains(verdict.SurfaceId))
            .OrderBy(verdict => verdict.SurfaceId, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Proofs that name a surface the sweep now evidences, or no longer measures at all. Either way the
    /// claim is stale and must be deleted rather than left standing.
    /// </summary>
    public static IReadOnlyList<string> Stale(
        IReadOnlyList<CounterfactualResult> verdicts,
        IReadOnlyList<ImpossibilityProof> proofs
    )
    {
        ArgumentNullException.ThrowIfNull(verdicts);
        ArgumentNullException.ThrowIfNull(proofs);
        var evidenced = verdicts
            .Where(verdict =>
                verdict.Verdict
                    is CounterfactualVerdict.Evidenced
                        or CounterfactualVerdict.RequiredByDtd
                        or CounterfactualVerdict.RequiredByLoader
                        or CounterfactualVerdict.EvidencedJointly
            )
            .Select(verdict => verdict.SurfaceId)
            .ToHashSet(StringComparer.Ordinal);
        var measured = verdicts.Select(verdict => verdict.SurfaceId).ToHashSet(StringComparer.Ordinal);
        return proofs
            .Where(proof => evidenced.Contains(proof.SurfaceId) || !measured.Contains(proof.SurfaceId))
            .Select(proof => proof.SurfaceId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }
}
