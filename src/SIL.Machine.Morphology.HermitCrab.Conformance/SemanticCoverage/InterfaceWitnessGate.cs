#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// One severance run: <paramref name="Element"/>/<paramref name="Attribute"/> emptied out of every
/// occurrence in <paramref name="FixtureId"/>'s grammar, then diffed against that fixture's baseline --
/// the same counterfactual shape <see cref="CounterfactualGate"/> already uses for a unit surface,
/// applied to a declared <see cref="InterfaceInventoryLedger.Row"/> instead. A row in
/// <see cref="InterfaceInventoryLedger"/> being <c>present</c> (the attribute resolves to a real IDREF
/// somewhere) is a structural fact; this is the semantic one -- whether severing it ever changes a
/// word's parse.
/// </summary>
public sealed record InterfaceWitnessResult(
    string Element,
    string Attribute,
    string FixtureId,
    CounterfactualVerdict Verdict,
    string Mutation,
    string Delta,
    string? ExampleWord = null,
    string? ExampleOutcome = null,
    CounterexampleKind CounterexampleKind = CounterexampleKind.None,
    string? CounterexampleOutcome = null
);

/// <summary>
/// Runs <see cref="InterfaceWitnessResult"/> severance mutations. Deliberately separate from
/// <see cref="InterfaceInventoryLedger.Compute"/> (which stays a cheap, DTD+presence-only read run on
/// every ordinary test pass): a severance run re-parses every word of every fixture the interface is
/// present in, the same order of cost as <see cref="CounterfactualLedger.Sweep"/>, and paying that on
/// every normal build would repeat this repo's own established mistake of taxing ordinary work with an
/// expensive gate.
/// </summary>
public static class InterfaceWitnessGate
{
    /// <summary>
    /// Whether the DTD declares <paramref name="attribute"/> on <paramref name="element"/> as
    /// <c>#REQUIRED</c> -- read back from <see cref="DtdInventoryReader"/>'s own "attribute" surface
    /// rather than re-parsing the DTD by hand.
    /// </summary>
    public static bool IsRequiredByDtd(SemanticInventory inventory, string element, string attribute)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        InventorySurface? surface = inventory.Surfaces.FirstOrDefault(s =>
            s.Kind == "attribute" && s.Parent == element && s.Name == attribute
        );
        if (surface?.Value is null)
            return false;

        foreach (string field in surface.Value.Split(';'))
        {
            if (field.StartsWith("default=", StringComparison.Ordinal))
                return field["default=".Length..] == "#REQUIRED";
        }
        return false;
    }

    /// <summary>
    /// Removes every occurrence of <paramref name="attribute"/> on every <paramref name="element"/> in
    /// a deep copy of <paramref name="grammar"/>. Returns null if the fixture's grammar carries no such
    /// occurrence at all -- the interface is not present there, so there is nothing to sever.
    /// </summary>
    public static XDocument? Sever(XDocument grammar, string element, string attribute, out int removedCount)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        var copy = new XDocument(grammar);
        removedCount = 0;
        foreach (XElement owner in copy.Descendants(element))
        {
            XAttribute? candidate = owner.Attribute(attribute);
            if (candidate is null || string.IsNullOrEmpty(candidate.Value))
                continue;
            candidate.Remove();
            removedCount++;
        }

        return removedCount == 0 ? null : copy;
    }

    // Shares CounterfactualGate's constant rather than keeping a second copy that could drift.
    private static readonly TimeSpan DefaultTimeout = CounterfactualGate.DefaultTimeout;

    /// <summary>
    /// Severs <paramref name="element"/>.<paramref name="attribute"/> in <paramref name="fixture"/>'s
    /// grammar and diffs the reparse against <paramref name="baseline"/>. Both baseline and mutant run
    /// through <see cref="CounterfactualGate.EvaluateWithTimeout"/>'s killable child process -- unlike
    /// a unit-surface neutralization (which trusts an in-process, untimed baseline because it is the
    /// fixture's own unmodified grammar), this corpus includes deliberately pathological and
    /// expect_crash fixtures, so neither run gets an unprotected path here.
    /// </summary>
    public static InterfaceWitnessResult Evaluate(
        Fixture fixture,
        string element,
        string attribute,
        SemanticInventory inventory,
        IReadOnlyList<string> baseline,
        string scratchDirectory,
        TimeSpan? timeout = null,
        Action<string, long>? onWordTimed = null
    )
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(baseline);

        XDocument grammar = XDocument.Load(fixture.GrammarPath);
        XDocument? mutant = Sever(grammar, element, attribute, out int removedCount);
        if (mutant is null)
        {
            return new InterfaceWitnessResult(
                element,
                attribute,
                fixture.Id,
                CounterfactualVerdict.Unobservable,
                "none",
                "the fixture does not carry this attribute"
            );
        }

        string[] words = fixture.Words.Words.Select(word => word.Word).ToArray();
        TimeSpan effectiveTimeout = timeout ?? DefaultTimeout;
        string mutation = $"removed {attribute} from {removedCount} <{element}> element(s)";

        Directory.CreateDirectory(scratchDirectory);
        string mutatedPath = Path.Combine(scratchDirectory, $"witness-{Guid.NewGuid():N}.xml");
        string? dtdSource = Path.Combine(Path.GetDirectoryName(fixture.GrammarPath)!, "HermitCrabInput.dtd");
        if (File.Exists(dtdSource))
            File.Copy(dtdSource, Path.Combine(scratchDirectory, "HermitCrabInput.dtd"), overwrite: true);

        try
        {
            IReadOnlyList<string> mutated;
            try
            {
                mutant.Save(mutatedPath);
                mutated = CounterfactualGate.EvaluateWithTimeout(mutatedPath, words, effectiveTimeout, onWordTimed);
            }
            catch (TimeoutException)
            {
                return new InterfaceWitnessResult(
                    element,
                    attribute,
                    fixture.Id,
                    CounterfactualVerdict.Timeout,
                    mutation,
                    $"the mutant did not terminate within {effectiveTimeout.TotalSeconds:0}s"
                );
            }
            catch (Exception ex)
            {
                string loadFailure = Summarize($"{ex.GetType().Name}: {ex.Message}");
                CounterfactualVerdict verdict = IsRequiredByDtd(inventory, element, attribute)
                    ? CounterfactualVerdict.RequiredByDtd
                    : CounterfactualVerdict.RequiredByLoader;
                return new InterfaceWitnessResult(
                    element,
                    attribute,
                    fixture.Id,
                    verdict,
                    mutation,
                    loadFailure,
                    ExampleWord: words.Length != 0 ? words[0] : null,
                    ExampleOutcome: words.Length != 0 ? baseline[0] : null,
                    CounterexampleKind: CounterexampleKind.LoadFailure,
                    CounterexampleOutcome: loadFailure
                );
            }

            for (int i = 0; i < words.Length; i++)
            {
                if (baseline[i] != mutated[i])
                {
                    return new InterfaceWitnessResult(
                        element,
                        attribute,
                        fixture.Id,
                        CounterfactualVerdict.Evidenced,
                        mutation,
                        $"'{words[i]}': {baseline[i]} -> {mutated[i]}",
                        ExampleWord: words[i],
                        ExampleOutcome: baseline[i],
                        CounterexampleKind: CounterexampleKind.Word,
                        CounterexampleOutcome: mutated[i]
                    );
                }
            }

            return new InterfaceWitnessResult(
                element,
                attribute,
                fixture.Id,
                CounterfactualVerdict.Unobservable,
                mutation,
                $"all {words.Length} word(s) unchanged"
            );
        }
        finally
        {
            try
            {
                if (File.Exists(mutatedPath))
                    File.Delete(mutatedPath);
            }
            catch (IOException)
            {
                // Mirrors CounterfactualGate.RunMutation: a killed timed-out worker may still hold the
                // file open, and blocking on that thread is worse than leaving one scratch file behind.
            }
        }
    }

    private static string Summarize(string message)
    {
        string single = message.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return single.Length <= 120 ? single : single[..120] + "...";
    }
}
