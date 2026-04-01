using SIL.Machine.Corpora;

namespace SIL.Machine.QualityEstimation
{
    public class ScriptureSegmentUsability : ScriptureChapterUsability
    {
        public ScriptureSegmentUsability(
            ScriptureRef scriptureRef,
            UsabilityLabel label,
            double projectedChrF3,
            double usability
        )
            : base(scriptureRef.Book, scriptureRef.ChapterNum, label, projectedChrF3, usability)
        {
            ScriptureRef = scriptureRef;
        }

        public ScriptureRef ScriptureRef { get; }
    }
}
