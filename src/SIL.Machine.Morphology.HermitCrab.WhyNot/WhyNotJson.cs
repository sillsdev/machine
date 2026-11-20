using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SIL.Machine.Morphology.HermitCrab.WhyNot;

/// <summary>
/// Writes the "whynot.v1" document. The name is engine-neutral; the shape is compatible with
/// pangloss.whynot.v1 so one consumer can read either engine. Morphs are {morpheme, allomorph} using the
/// grammar's own ids; an id that cannot be known is omitted and named in "notes". FieldWorks GUIDs are never
/// written, since the grammar file does not carry them.
/// </summary>
public static class WhyNotJson
{
    public const string SchemaName = "whynot.v1";

    public static string Serialize(WhyNotResult result, GrammarIds ids, bool indented = true)
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
            Write(w, result, ids);
        return new UTF8Encoding(false).GetString(stream.ToArray());
    }

    private static void Write(Utf8JsonWriter w, WhyNotResult result, GrammarIds ids)
    {
        var notes = new List<string>();
        w.WriteStartObject();
        w.WriteString("schema", SchemaName);
        w.WriteString("word", result.Word);
        w.WriteStartArray("expect");
        foreach (ExpectedMorph e in result.Expect)
        {
            w.WriteStartObject();
            w.WriteString("morpheme", e.MorphemeId);
            string allomorph =
                e.AllomorphId ?? (e.AllomorphIndex is int index ? ids.AllomorphId(e.MorphemeId, index) : null);
            if (allomorph != null)
                w.WriteString("allomorph", allomorph);
            w.WriteEndObject();
        }
        w.WriteEndArray();
        Failure failure = result.Results.Count > 0 ? result.Results[0].Failure : null;
        string incomplete = failure == null ? null : MissingRequiredField(failure, ids);
        w.WriteString("result", incomplete != null ? "unknown" : result.Outcome.ToString().ToLowerInvariant());

        if (result.Analysis != null)
            WriteMorphs(w, "analysis", result.Analysis, ids, notes);

        if (failure != null && incomplete == null)
        {
            w.WritePropertyName("failure");
            WriteFailure(w, failure, ids, notes);
            w.WriteNumber("alternatives", result.Results.Count - 1);
        }

        if (incomplete != null)
        {
            notes.Add(incomplete);
        }
        string stop = incomplete != null ? "failure_not_expressible" : result.StopReason;
        if (stop != null)
        {
            w.WriteStartObject("stop");
            w.WriteString("reason", stop);
            w.WriteEndObject();
        }
        if (notes.Count > 0)
        {
            w.WriteStartArray("notes");
            foreach (string note in notes)
                w.WriteStringValue(note);
            w.WriteEndArray();
        }
        w.WriteEndObject();
    }

    // The failure shapes the schema requires fields for; a failure that cannot fill one is reported as unknown
    // rather than as an invalid row.
    private static string MissingRequiredField(Failure f, GrammarIds ids)
    {
        bool partnerMayLackAllomorph = f.HcReason == "MorphemeCoOccurrenceRules";
        for (int i = 0; i < f.Morphs.Count; i++)
        {
            MorphRef m = f.Morphs[i];
            if (partnerMayLackAllomorph && i == 1)
                continue;
            if (m.AllomorphIndex < 0 || ids.AllomorphId(m.MorphemeId, m.AllomorphIndex) == null)
                return $"failure morph '{m.MorphemeId}' has no allomorph id in the grammar file";
        }
        if (
            f.Kind == "environment_unmet"
            && (
                f.EnvironmentAllomorph == null
                || ids.AllomorphId(f.EnvironmentAllomorph.MorphemeId, f.EnvironmentAllomorph.AllomorphIndex) == null
            )
        )
        {
            return "the allomorph whose environment failed has no id in the grammar file";
        }
        if (f.Kind == "feature_clash" && f.Features.Count == 0)
            return "the feature clash carries no feature paths";
        return null;
    }

    private static void WriteFailure(Utf8JsonWriter w, Failure f, GrammarIds ids, List<string> notes)
    {
        w.WriteStartObject();
        w.WriteString("kind", f.Kind);
        w.WriteString("hc_reason", f.HcReason);
        w.WriteString("plain", f.Plain);
        if (f.Position is int position)
            w.WriteNumber("position", position);
        w.WriteString("provenance", f.Provenance);

        if (f.Morphs.Count > 0)
            WriteMorphs(w, "morphs", f.Morphs, ids, notes);
        if (f.EnvironmentAllomorph != null)
        {
            // Allomorph.Environments is an unordered set and the check fails only when every alternative fails,
            // so no environment index is reported.
            string allomorphId = ids.AllomorphId(
                f.EnvironmentAllomorph.MorphemeId,
                f.EnvironmentAllomorph.AllomorphIndex
            );
            if (allomorphId == null)
            {
                notes.Add(UnknownAllomorph(f.EnvironmentAllomorph));
            }
            else
            {
                w.WriteStartObject("environment");
                w.WriteString("allomorph_id", allomorphId);
                w.WriteEndObject();
            }
        }
        if (f.StemName?.Name != null)
        {
            string stemNameId = ids.StemNameId(f.StemName);
            if (stemNameId == null)
                notes.Add($"stem name '{f.StemName.Name}' has no unique id in the grammar file");
            else
                w.WriteString("stem_name", stemNameId);
        }
        if (f.Features.Count > 0)
        {
            w.WriteStartArray("features");
            foreach (FeatureMismatch m in f.Features)
            {
                w.WriteStartObject();
                w.WriteString("path", m.Path);
                w.WriteString("required", m.Required);
                w.WriteString("actual", m.Actual ?? "(unspecified)");
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        w.WriteEndObject();
    }

    private static void WriteMorphs(
        Utf8JsonWriter w,
        string name,
        IEnumerable<MorphRef> morphs,
        GrammarIds ids,
        List<string> notes
    )
    {
        w.WriteStartArray(name);
        foreach (MorphRef m in morphs)
        {
            w.WriteStartObject();
            w.WriteString("morpheme", m.MorphemeId);
            if (m.AllomorphIndex >= 0)
            {
                string allomorphId = ids.AllomorphId(m.MorphemeId, m.AllomorphIndex);
                if (allomorphId == null)
                    notes.Add(UnknownAllomorph(m));
                else
                    w.WriteString("allomorph", allomorphId);
            }
            w.WriteEndObject();
        }
        w.WriteEndArray();
    }

    private static string UnknownAllomorph(MorphRef m) =>
        $"allomorph {m.AllomorphIndex} of morpheme '{m.MorphemeId}' has no id in the grammar file";
}
