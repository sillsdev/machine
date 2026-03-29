namespace SIL.Machine.QualityEstimation
{
    public class TextUsability : UsabilityBase
    {
        public TextUsability(string textId, UsabilityLabel label, double projectedChrF3, double usability)
            : base(label, projectedChrF3, usability)
        {
            TextId = textId;
        }

        public string TextId { get; }
    }
}
