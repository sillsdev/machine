namespace SIL.Machine.QualityEstimation
{
    public class ScriptureBookUsability : UsabilityBase
    {
        public ScriptureBookUsability(
            string book,
            UsabilityLabel label,
            double projectedChrF3,
            double usability,
            double confidence
        )
            : base(label, projectedChrF3, usability, confidence)
        {
            Book = book;
        }

        public string Book { get; }
    }
}
