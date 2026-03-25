namespace SIL.Machine.QualityEstimation
{
    public class VerseUsability : ChapterUsability
    {
        public VerseUsability(
            string book,
            int chapter,
            string verse,
            UsabilityLabel label,
            double projectedChrF3,
            double usability
        )
            : base(book, chapter, label, projectedChrF3, usability)
        {
            Verse = verse;
        }

        public string Verse { get; }
    }
}
