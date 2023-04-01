using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
    public class TranslationResult
    {
        private readonly double[] _confidences;

        public TranslationResult(
            string translation,
            IEnumerable<string> sourceTokens,
            IEnumerable<string> targetTokens,
            IEnumerable<double> confidences,
            IEnumerable<TranslationSources> sources,
            WordAlignmentMatrix alignment,
            IEnumerable<Phrase> phrases
        )
        {
            Translation = translation;
            SourceTokens = sourceTokens.ToArray();
            TargetTokens = targetTokens.ToArray();
            _confidences = confidences.ToArray();
            if (Confidences.Count != TargetTokens.Count)
            {
                throw new ArgumentException(
                    "The confidences must be the same length as the target segment.",
                    nameof(confidences)
                );
            }
            Sources = sources.ToArray();
            if (Sources.Count != TargetTokens.Count)
            {
                throw new ArgumentException(
                    "The sources must be the same length as the target segment.",
                    nameof(sources)
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

            Phrases = phrases.ToArray();
        }

        public string Translation { get; }
        public IReadOnlyList<string> SourceTokens { get; }
        public IReadOnlyList<string> TargetTokens { get; }
        public IReadOnlyList<double> Confidences => _confidences;
        public IReadOnlyList<TranslationSources> Sources { get; }
        public WordAlignmentMatrix Alignment { get; }
        public IReadOnlyList<Phrase> Phrases { get; }

        internal void SetConfidence(int i, double score)
        {
            _confidences[i] = score;
        }
    }
}
