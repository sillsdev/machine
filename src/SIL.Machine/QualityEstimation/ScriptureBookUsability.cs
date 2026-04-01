namespace SIL.Machine.QualityEstimation
{
    public class ScriptureBookUsability : UsabilityBase
    {
        public ScriptureBookUsability(string book, UsabilityLabel label, double projectedChrF3, double usability)
            : base(label, projectedChrF3, usability)
        {
            Book = book;
        }

        public string Book { get; }
    }
}
