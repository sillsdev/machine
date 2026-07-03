using System.Linq;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Builds the bucket keys/examples that InstrumentedRule.RecordBucket stores, so a corpus-wide stats run
    /// can answer questions like "does this rule only ever fire on verbs" or "does this allomorph only ever
    /// attach to a bare stem" from real parse traffic rather than re-reading the grammar XML by hand.
    /// RootAllomorph/StemName are only meaningfully populated on the synthesis side (analysis doesn't know
    /// the root's lexical identity until the derivation bottoms out), so those groups read "(none)" for most
    /// analysis-direction calls -- that is itself useful signal, not a bug.
    /// </summary>
    internal static class RuleStatsHelper
    {
        public const string CategoryGroup = "category";
        public const string StemNameGroup = "stemName";
        public const string AllomorphGroup = "allomorph";
        public const string RootDirectGroup = "rootDirect";
        public const string SubruleGroup = "subrule";
        public const string NonHeadCategoryGroup = "nonHeadCategory";

        public static string Category(Word word)
        {
            FeatureSymbol pos = word.SyntacticFeatureStruct?.PartsOfSpeech().FirstOrDefault();
            return pos?.ID ?? "(none)";
        }

        public static string StemName(Word word)
        {
            return word.RootAllomorph?.StemName?.Name ?? "(none)";
        }

        // "true" = this application's input had no morphological rules recorded on it yet -- for synthesis
        // that means the affix/phonological rule fired directly against the bare stem; for analysis it means
        // this was the innermost/first rule unapplied. Either reading answers "does this only ever touch the
        // stem, or does it also apply once other affixes are already present."
        public static string IsRootDirect(Word word)
        {
            return word.MorphologicalRules.Any() ? "false" : "true";
        }

        public static string Example(Word word)
        {
            return word.ToString();
        }
    }
}
