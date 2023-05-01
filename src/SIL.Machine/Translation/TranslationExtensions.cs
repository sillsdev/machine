using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Translation
{
    public static class TranslationExtensions
    {
        public static IEnumerable<TranslationSuggestion> GetSuggestions(
            this ITranslationSuggester suggester,
            InteractiveTranslator translator
        )
        {
            return translator
                .GetCurrentResults()
                .Select(
                    r => suggester.GetSuggestion(translator.PrefixWordRanges.Count, translator.IsLastWordComplete, r)
                );
        }

        public static IEnumerable<TranslationSuggestion> GetSuggestions(
            this ITranslationSuggester suggester,
            InteractiveTranslator translator,
            ITruecaser truecaser,
            IDetokenizer<string, string> detokenizer = null
        )
        {
            return translator
                .GetCurrentResults()
                .Select(
                    r =>
                        suggester.GetSuggestion(
                            translator.PrefixWordRanges.Count,
                            translator.IsLastWordComplete,
                            truecaser.Truecase(r, detokenizer)
                        )
                );
        }

        public static Dictionary<string, Dictionary<string, double>> GetTranslationTable(
            this IWordAlignmentModel model,
            double threshold = 0
        )
        {
            var results = new Dictionary<string, Dictionary<string, double>>();
            string[] sourceWords = model.SourceWords.ToArray();
            string[] targetWords = model.TargetWords.ToArray();
            for (int i = 0; i < sourceWords.Length; i++)
            {
                var row = new Dictionary<string, double>();
                foreach ((int j, double score) in model.GetTranslations(i, threshold))
                    row[targetWords[j]] = score;
                results[sourceWords[i]] = row;
            }
            return results;
        }

        public static WordAlignmentMatrix Align(this IWordAligner aligner, ParallelTextRow row)
        {
            WordAlignmentMatrix alignment = aligner.Align(row.SourceSegment, row.TargetSegment);
            WordAlignmentMatrix knownAlignment = row.CreateAlignmentMatrix();
            if (knownAlignment != null)
            {
                knownAlignment.PrioritySymmetrizeWith(alignment);
                alignment = knownAlignment;
            }
            return alignment;
        }

        public static WordAlignmentMatrix CreateAlignmentMatrix(this ParallelTextRow segment)
        {
            if (segment.AlignedWordPairs == null)
                return null;

            var matrix = new WordAlignmentMatrix(segment.SourceSegment.Count, segment.TargetSegment.Count);
            foreach (AlignedWordPair wordPair in segment.AlignedWordPairs)
                matrix[wordPair.SourceIndex, wordPair.TargetIndex] = true;

            return matrix;
        }

        public static string GetAlignmentString(
            this IWordAlignmentModel model,
            ParallelTextRow row,
            bool includeScores = true
        )
        {
            WordAlignmentMatrix alignment = model.Align(row);
            return alignment.ToString(model, row.SourceSegment, row.TargetSegment, includeScores);
        }

        public static string GetGizaFormatString(this IWordAligner aligner, ParallelTextRow row)
        {
            WordAlignmentMatrix alignment = aligner.Align(row);
            return alignment.ToGizaFormat(row.SourceSegment, row.TargetSegment);
        }

        public static void TrainSegment(this ITruecaser truecaser, TextRow segment)
        {
            truecaser.TrainSegment(segment.Segment, segment.IsSentenceStart);
        }

        public static string Capitalize(this string sentence)
        {
            if (string.IsNullOrEmpty(sentence))
                return sentence;
            return char.ToUpperInvariant(sentence[0]) + sentence.Substring(1);
        }

        public static TranslationResult Truecase(
            this ITruecaser truecaser,
            TranslationResult result,
            IDetokenizer<string, string> detokenizer = null
        )
        {
            if (detokenizer == null)
                detokenizer = WhitespaceDetokenizer.Instance;
            IReadOnlyList<string> targetTokens = truecaser.Truecase(result.TargetTokens);
            return new TranslationResult(
                detokenizer.Detokenize(targetTokens),
                result.SourceTokens,
                targetTokens,
                result.Confidences,
                result.Sources,
                result.Alignment,
                result.Phrases
            );
        }

        public static WordGraph Truecase(this ITruecaser truecaser, WordGraph wordGraph)
        {
            var newArcs = new List<WordGraphArc>();
            foreach (WordGraphArc arc in wordGraph.Arcs)
            {
                newArcs.Add(
                    new WordGraphArc(
                        arc.PrevState,
                        arc.NextState,
                        arc.Score,
                        truecaser.Truecase(arc.TargetTokens),
                        arc.Alignment,
                        arc.SourceSegmentRange,
                        arc.Sources,
                        arc.Confidences
                    )
                );
            }
            return new WordGraph(wordGraph.SourceTokens, newArcs, wordGraph.FinalStates, wordGraph.InitialStateScore);
        }

        public static double GetAvgTranslationScore(
            this IWordAlignmentModel model,
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            WordAlignmentMatrix waMatrix
        )
        {
            var scores = new List<double>();
            foreach (AlignedWordPair wordPair in waMatrix.ToAlignedWordPairs(includeNull: true))
            {
                string sourceWord = wordPair.SourceIndex == -1 ? null : sourceSegment[wordPair.SourceIndex];
                string targetWord = wordPair.TargetIndex == -1 ? null : targetSegment[wordPair.TargetIndex];
                scores.Add(model.GetTranslationScore(sourceWord, targetWord));
            }
            return scores.Count > 0 ? scores.Average() : 0;
        }
    }
}
