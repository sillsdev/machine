#nullable enable

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>Which generator produced a <see cref="CoverageItem"/>.</summary>
public enum CoverageItemKind
{
    Surface,
    Ordering,
    Interaction,
}

/// <summary>
/// How a counter-example was obtained. Never summed across kinds when reporting coverage: a load
/// failure and a word-level contrast are different strengths of evidence, and blending them into one
/// count hides which strength a given item actually reached.
/// </summary>
public enum CounterexampleKind
{
    /// <summary>The mutant grammar still loads, and a specific word's parse outcome changed.</summary>
    Word,

    /// <summary>The mutant grammar would not load at all.</summary>
    LoadFailure,

    /// <summary>No counter-example exists; the item can only be resolved by a <see cref="Proof"/>.</summary>
    None,
}

/// <summary>
/// One line of the inventory: something the pipeline has committed to cover. Generated, never
/// hand-maintained. <paramref name="Fixture"/> names where the item is expected to be evidenced,
/// not necessarily where it ends up evidenced.
/// </summary>
public sealed record CoverageItem(string Id, CoverageItemKind Kind, string Origin, string Fixture);

/// <summary>
/// The example and counter-example for one <see cref="CoverageItem"/>, captured at the moment a
/// mutant's outcome was compared against the baseline -- never reconstructed afterward from a
/// <see cref="CounterfactualResult.Delta"/> string.
/// </summary>
public sealed record Evidence(
    string ItemId,
    string Fixture,
    string? ExampleWord,
    string? ExampleOutcome,
    CounterexampleKind CounterexampleKind,
    string? CounterexampleOutcome,
    string Mutation,
    CounterfactualVerdict Verdict
)
{
    /// <summary>
    /// Lifts the structural fields a <see cref="CounterfactualResult"/> already carries into
    /// <see cref="Evidence"/> for <paramref name="itemId"/>, rather than re-deriving them from
    /// <see cref="CounterfactualResult.Delta"/>'s free text.
    /// </summary>
    public static Evidence FromCounterfactualResult(string itemId, CounterfactualResult result) =>
        new(
            itemId,
            result.FixtureId,
            result.ExampleWord,
            result.ExampleOutcome,
            result.CounterexampleKind,
            result.CounterexampleOutcome,
            result.Mutation,
            result.Verdict
        );
}

/// <summary>
/// A claim that evidence for a <see cref="CoverageItem"/> is impossible, re-verified at gate time
/// rather than trusted as prose. <paramref name="Kind"/> names a check the gate recomputes -- see
/// <see cref="ImpossibilityProofs"/> for the concrete checks behind each kind.
/// </summary>
public sealed record Proof(string ItemId, string Kind, string Check);
