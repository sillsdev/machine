using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
    public static class TranslationExtensions
    {
        public static async Task<IReadOnlyList<string>> TranslateWordAsync(
            this ITranslationEngine engine,
            string sourceWord
        )
        {
            TranslationResult result = await engine.TranslateAsync(new[] { sourceWord }).ConfigureAwait(false);
            if (result.WordSources.Any(s => s == TranslationSources.None))
                return new string[0];
            return result.TargetSegment;
        }

        public static IEnumerable<TranslationSuggestion> GetSuggestions(
            this ITranslationSuggester suggester,
            InteractiveTranslator translator
        )
        {
            return translator
                .GetCurrentResults()
                .Select(r => suggester.GetSuggestion(translator.Prefix.Count, translator.IsLastWordComplete, r));
        }

        public static IEnumerable<TranslationSuggestion> GetSuggestions(
            this ITranslationSuggester suggester,
            InteractiveTranslator translator,
            ITruecaser truecaser
        )
        {
            return translator
                .GetCurrentResults()
                .Select(
                    r =>
                        suggester.GetSuggestion(
                            translator.Prefix.Count,
                            translator.IsLastWordComplete,
                            truecaser.Truecase(r)
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

        public static TranslationResult Truecase(this ITruecaser truecaser, TranslationResult result)
        {
            return new TranslationResult(
                result.SourceSegmentLength,
                truecaser.Truecase(result.TargetSegment),
                result.WordConfidences,
                result.WordSources,
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
                        truecaser.Truecase(arc.Words),
                        arc.Alignment,
                        arc.SourceSegmentRange,
                        arc.WordSources,
                        arc.WordConfidences
                    )
                );
            }
            return new WordGraph(newArcs, wordGraph.FinalStates, wordGraph.InitialStateScore);
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
