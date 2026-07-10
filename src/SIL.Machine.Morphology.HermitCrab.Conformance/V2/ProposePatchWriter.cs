using System.Collections.Generic;
using System.IO;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.V2;

/// <summary>
/// <c>--propose</c>: on a self-check signature mismatch, prints the <c>words.yaml</c> patch that
/// would reconcile the fixture with what the oracle actually produced, per
/// docs/conformance-language-suite-plan.md ("a `--propose` mode prints the would-be YAML patch for
/// a human to accept. No mode ever writes the file."). This class only ever writes to the
/// <see cref="TextWriter"/> it is given -- it has no file-write call anywhere, by construction, so
/// there is no code path here that could accidentally start writing words.yaml back out.
/// </summary>
public static class ProposePatchWriter
{
    public static void WriteSignatureProposal(
        TextWriter output,
        FixtureV2 fixture,
        WordEntry word,
        List<string> actualEntries
    )
    {
        output.WriteLine();
        output.WriteLine($"# --propose: {fixture.Id}/words.yaml -- word '{word.Word}' signature mismatch");
        output.WriteLine("# Proposed replacement entry (rules:/gloss: left as-is; fill in by hand):");
        if (actualEntries.Count == 0)
        {
            output.WriteLine($"  - word: {word.Word}");
            output.WriteLine($"    note: {word.Note}");
            output.WriteLine("    expect_fail: true");
        }
        else
        {
            output.WriteLine($"  - word: {word.Word}");
            output.WriteLine($"    note: {word.Note}");
            output.WriteLine("    parses:");
            foreach (string entry in actualEntries)
            {
                output.WriteLine($"      - signature: \"{entry}\"");
                output.WriteLine("        rules: []  # TODO: fill in from the trace");
            }
        }
        output.WriteLine();
    }
}
