using System.Collections.Generic;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab.WhyNot;

public record FeatureMismatch(string Path, string Required, string Actual);

public static class FeatureStructDiff
{
    /// <summary>
    /// One entry per feature path where <paramref name="actual"/> does not satisfy <paramref name="required"/>.
    /// </summary>
    public static IReadOnlyList<FeatureMismatch> Diff(FeatureStruct required, FeatureStruct actual)
    {
        var result = new List<FeatureMismatch>();
        Collect(required, actual, "", result);
        return result;
    }

    private static void Collect(
        FeatureStruct required,
        FeatureStruct actual,
        string prefix,
        List<FeatureMismatch> result
    )
    {
        foreach (Feature feature in required.Features)
        {
            string segment = Segment(feature);
            string path = prefix.Length == 0 ? segment : prefix + "/" + segment;
            FeatureValue requiredValue = required.GetValue(feature);
            FeatureValue actualValue = null;
            actual?.TryGetValue(feature, out actualValue);

            if (requiredValue is FeatureStruct requiredFs)
            {
                if (actualValue is FeatureStruct actualFs)
                    Collect(requiredFs, actualFs, path, result);
                else if (!requiredFs.IsEmpty)
                    Collect(requiredFs, null, path, result);
            }
            else if (actualValue == null)
            {
                result.Add(new FeatureMismatch(path, requiredValue.ToString(), null));
            }
            else if (!Satisfies(requiredValue, actualValue))
            {
                result.Add(new FeatureMismatch(path, requiredValue.ToString(), actualValue.ToString()));
            }
        }
    }

    // HermitCrab assigns the head and foot features a fresh id per process, so they get stable names.
    public static string Segment(Feature feature)
    {
        if (feature.ID == SyntacticFeatureSystem.HeadID)
            return "head";
        return feature.ID == SyntacticFeatureSystem.FootID ? "foot" : feature.ID;
    }

    private static bool Satisfies(FeatureValue required, FeatureValue actual)
    {
        if (required is SymbolicFeatureValue requiredSymbols && actual is SymbolicFeatureValue actualSymbols)
            return requiredSymbols.IsSupersetOf(actualSymbols);
        return required.ValueEquals(actual);
    }
}
