using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
    public class TranslationResult
    {
        public TranslationResult(
            int sourceSegmentLength,
            IEnumerable<string> targetSegment,
            IEnumerable<double> confidences,
            IEnumerable<TranslationSources> sources,
            WordAlignmentMatrix alignment,
            IEnumerable<Phrase> phrases
        )
        {
            SourceSegmentLength = sourceSegmentLength;
            TargetSegment = targetSegment.ToArray();
            WordConfidences = confidences.ToArray();
            if (WordConfidences.Count != TargetSegment.Count)
            {
                throw new ArgumentException(
                    "The confidences must be the same length as the target segment.",
                    nameof(confidences)
                );
            }
            WordSources = sources.ToArray();
            if (WordSources.Count != TargetSegment.Count)
            {
                throw new ArgumentException(
                    "The sources must be the same length as the target segment.",
                    nameof(sources)
                );
            }
            Alignment = alignment;
            if (Alignment.RowCount != sourceSegmentLength)
            {
                throw new ArgumentException(
                    "The alignment source length must be the same length as the source segment.",
                    nameof(alignment)
                );
            }
            if (Alignment.ColumnCount != TargetSegment.Count)
            {
                throw new ArgumentException(
                    "The alignment target length must be the same length as the target segment.",
                    nameof(alignment)
                );
            }

            Phrases = phrases.ToArray();
        }

        public int SourceSegmentLength { get; }
        public IReadOnlyList<string> TargetSegment { get; }
        public IReadOnlyList<double> WordConfidences { get; }
        public IReadOnlyList<TranslationSources> WordSources { get; }
        public WordAlignmentMatrix Alignment { get; }
        public IReadOnlyList<Phrase> Phrases { get; }

        public TranslationResult Merge(double threshold, TranslationResult otherResult)
        {
            var mergedTargetSegment = new List<string>();
            var mergedConfidences = new List<double>();
            var mergedSources = new List<TranslationSources>();
            var mergedAlignment = new HashSet<Tuple<int, int>>();
            for (int j = 0; j < TargetSegment.Count; j++)
            {
                int[] sourceIndices = Alignment.GetColumnAlignedIndices(j).ToArray();
                if (sourceIndices.Length == 0)
                {
                    // target word doesn't align with anything
                    mergedTargetSegment.Add(TargetSegment[j]);
                    mergedConfidences.Add(WordConfidences[j]);
                    mergedSources.Add(WordSources[j]);
                }
                else
                {
                    // target word aligns with some source words
                    if (WordConfidences[j] >= threshold)
                    {
                        // use target word of this result
                        mergedTargetSegment.Add(TargetSegment[j]);
                        mergedConfidences.Add(WordConfidences[j]);
                        TranslationSources sources = WordSources[j];
                        foreach (int i in sourceIndices)
                        {
                            // combine sources for any words that both this result
                            // and the other result translated the same
                            foreach (int jOther in otherResult.Alignment.GetRowAlignedIndices(i))
                            {
                                TranslationSources otherSources = otherResult.WordSources[jOther];
                                if (
                                    otherSources != TranslationSources.None
                                    && otherResult.TargetSegment[jOther] == TargetSegment[j]
                                )
                                {
                                    sources |= otherSources;
                                }
                            }

                            mergedAlignment.Add(Tuple.Create(i, mergedTargetSegment.Count - 1));
                        }
                        mergedSources.Add(sources);
                    }
                    else
                    {
                        // use target words of other result
                        bool found = false;
                        foreach (int i in sourceIndices)
                        {
                            foreach (int jOther in otherResult.Alignment.GetRowAlignedIndices(i))
                            {
                                // look for any translated words from other result
                                TranslationSources otherSources = otherResult.WordSources[jOther];
                                if (otherSources != TranslationSources.None)
                                {
                                    mergedTargetSegment.Add(otherResult.TargetSegment[jOther]);
                                    mergedConfidences.Add(otherResult.WordConfidences[jOther]);
                                    mergedSources.Add(otherSources);
                                    mergedAlignment.Add(Tuple.Create(i, mergedTargetSegment.Count - 1));
                                    found = true;
                                }
                            }
                        }

                        if (!found)
                        {
                            // the other result had no translated words, so just use this result's target word
                            mergedTargetSegment.Add(TargetSegment[j]);
                            mergedConfidences.Add(WordConfidences[j]);
                            mergedSources.Add(WordSources[j]);
                            foreach (int i in sourceIndices)
                                mergedAlignment.Add(Tuple.Create(i, mergedTargetSegment.Count - 1));
                        }
                    }
                }
            }

            var alignment = new WordAlignmentMatrix(SourceSegmentLength, mergedTargetSegment.Count);
            foreach (Tuple<int, int> t in mergedAlignment)
                alignment[t.Item1, t.Item2] = true;
            return new TranslationResult(
                SourceSegmentLength,
                mergedTargetSegment,
                mergedConfidences,
                mergedSources,
                alignment,
                Phrases
            );
        }
    }
}
