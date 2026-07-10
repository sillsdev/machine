using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

public class ConstructCoverage
{
    public string Construct = "";
    public int FixtureCount;
    public bool HasNegative;
    public bool HasCrossCutting;
    public List<string> FixtureIds { get; } = new();
}

public class CoverageReportResult
{
    public List<ConstructCoverage> Constructs { get; } = new();
    public List<string> UnknownConstructsInManifests { get; } = new();

    public IEnumerable<ConstructCoverage> ZeroCoverage => Constructs.Where(c => c.FixtureCount == 0);
}

/// <summary>
/// Cross-references every fixture's manifest.json "constructs" entries against the checklist in
/// conformance/constructs.txt (a transcription of docs/conformance-framework-plan.md section 6),
/// per plan section 4.4. A manifest construct value that isn't in the checklist file is a soft
/// warning (plan section 10), not an error -- a genuinely new construct can be recorded before the
/// checklist catches up.
/// </summary>
public static class CoverageReport
{
    public static List<string> LoadConstructChecklist(string path)
    {
        var constructs = new List<string>();
        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#"))
                continue;
            constructs.Add(line);
        }
        return constructs;
    }

    public static CoverageReportResult Build(List<Fixture> fixtures, List<string> checklist)
    {
        var result = new CoverageReportResult();
        var byConstruct = new Dictionary<string, ConstructCoverage>();
        foreach (string construct in checklist)
        {
            var cc = new ConstructCoverage { Construct = construct };
            byConstruct[construct] = cc;
            result.Constructs.Add(cc);
        }

        foreach (Fixture fixture in fixtures)
        {
            foreach (string construct in fixture.Manifest.Constructs)
            {
                if (!byConstruct.TryGetValue(construct, out ConstructCoverage cc))
                {
                    // byConstruct is pre-seeded with every checklist entry above, so a miss here
                    // always means "not in constructs.txt" -- report it once, then tally it under
                    // its own ad hoc bucket (added to byConstruct immediately below) so a repeat of
                    // the same unknown construct hits the TryGetValue branch instead of this one,
                    // and the report doesn't silently drop coverage information for it either way.
                    result.UnknownConstructsInManifests.Add(construct);
                    cc = new ConstructCoverage { Construct = construct };
                    byConstruct[construct] = cc;
                    result.Constructs.Add(cc);
                }

                cc.FixtureCount++;
                cc.FixtureIds.Add(fixture.Id);
                if (fixture.Manifest.Category == "negative")
                    cc.HasNegative = true;
                if (fixture.Manifest.Category == "cross-cutting")
                    cc.HasCrossCutting = true;
            }
        }

        return result;
    }
}
