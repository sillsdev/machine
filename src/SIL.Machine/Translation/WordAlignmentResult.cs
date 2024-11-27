using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
    public class WordAlignmentResult
    {
        public WordAlignmentResult(
            IEnumerable<string> sourceTokens,
            IEnumerable<string> targetTokens,
            IEnumerable<double> confidences,
            WordAlignmentMatrix alignment
        )
        {
            SourceTokens = sourceTokens.ToArray();
            TargetTokens = targetTokens.ToArray();
            Confidences = confidences.ToArray();
            if (Confidences.Count != TargetTokens.Count)
            {
                throw new ArgumentException(
                    "The confidences must be the same length as the target segment.",
                    nameof(confidences)
                );
            }
            Alignment = alignment;
            if (Alignment.RowCount != SourceTokens.Count)
            {
                throw new ArgumentException(
                    "The alignment source length must be the same length as the source segment.",
                    nameof(alignment)
                );
            }
            if (Alignment.ColumnCount != TargetTokens.Count)
            {
                throw new ArgumentException(
                    "The alignment target length must be the same length as the target segment.",
                    nameof(alignment)
                );
            }
        }

        public IReadOnlyList<string> SourceTokens { get; }
        public IReadOnlyList<string> TargetTokens { get; }
        public IReadOnlyList<double> Confidences { get; }
        public WordAlignmentMatrix Alignment { get; }
    }
}
