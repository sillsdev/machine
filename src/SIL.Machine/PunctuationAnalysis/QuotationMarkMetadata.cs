namespace SIL.Machine.PunctuationAnalysis
{
    public class QuotationMarkMetadata
    {
        public string QuotationMark { get; }
        public int Depth { get; }
        public QuotationMarkDirection Direction { get; }
        public TextSegment TextSegment { get; }
        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }

        public QuotationMarkMetadata(
            string quotationMark,
            int depth,
            QuotationMarkDirection direction,
            TextSegment textSegment,
            int startIndex,
            int endIndex
        )
        {
            QuotationMark = quotationMark;
            Depth = depth;
            Direction = direction;
            TextSegment = textSegment;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public int Length => EndIndex - StartIndex;

        public void ShiftIndices(int shiftAmount)
        {
            StartIndex += shiftAmount;
            EndIndex += shiftAmount;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is QuotationMarkMetadata other))
            {
                return false;
            }
            return QuotationMark.Equals(other.QuotationMark)
                && Depth.Equals(other.Depth)
                && Direction.Equals(other.Direction)
                && TextSegment.Equals(other.TextSegment)
                && StartIndex.Equals(other.StartIndex)
                && EndIndex.Equals(other.EndIndex);
        }

        public override int GetHashCode()
        {
            int hashCode = 23;
            hashCode = hashCode * 31 + QuotationMark.GetHashCode();
            hashCode = hashCode * 31 + Depth.GetHashCode();
            hashCode = hashCode * 31 + Direction.GetHashCode();
            hashCode = hashCode * 31 + TextSegment.GetHashCode();
            hashCode = hashCode * 31 + StartIndex.GetHashCode();
            hashCode = hashCode * 31 + EndIndex.GetHashCode();
            return hashCode;
        }

        public void UpdateQuotationMark(QuoteConvention quoteConvention)
        {
            string updatedQuotationMark = quoteConvention.GetExpectedQuotationMark(Depth, Direction);
            if (updatedQuotationMark.Equals(QuotationMark))
                return;

            TextSegment.ReplaceSubstring(StartIndex, EndIndex, updatedQuotationMark);

            if (updatedQuotationMark.Length != QuotationMark.Length)
            {
                EndIndex += updatedQuotationMark.Length - QuotationMark.Length;
            }
        }
    }
}
