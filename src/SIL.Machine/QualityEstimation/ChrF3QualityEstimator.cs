using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;

namespace SIL.Machine.QualityEstimation
{
    /// <summary>
    /// Provides chrF3 quality estimation for pre-translations.
    /// </summary>
    public class ChrF3QualityEstimator
    {
        private readonly double _intercept;
        private readonly double _slope;

        /// <summary>
        /// Initializes a new instance of the ChrF3QualityEstimator class with the specified slope and intercept values.
        /// </summary>
        /// <param name="slope">The slope value used in the quality estimation calculation.</param>
        /// <param name="intercept">The intercept value used in the quality estimation calculation.</param>
        public ChrF3QualityEstimator(double slope, double intercept)
        {
            _slope = slope;
            _intercept = intercept;
        }

        /// <summary>
        /// The threshold values used to calculate the usability label for every book or text.
        /// </summary>
        public Thresholds BookThresholds { get; set; } = new Thresholds(greenThreshold: 0.745, yellowThreshold: 0.62);

        /// <summary>
        /// The threshold values used to calculate the usability label for every chapter.
        /// </summary>
        public Thresholds ChapterThresholds { get; set; } =
            new Thresholds(greenThreshold: 0.745, yellowThreshold: 0.62);

        /// <summary>
        /// The threshold values used to calculate the usability label for every segment.
        /// </summary>
        public Thresholds SegmentThresholds { get; set; } =
            new Thresholds(greenThreshold: 0.745, yellowThreshold: 0.62);

        /// <summary>
        /// The usable parameters to calculate the usable probabilities.
        /// </summary>
        public UsabilityParameters Usable { get; set; } = UsabilityParameters.Usable;

        /// <summary>
        /// The unusable parameters to calculate the usable probabilities.
        /// </summary>
        public UsabilityParameters Unusable { get; set; } = UsabilityParameters.Unusable;

        /// <summary>
        /// Estimate the quality of the pre-translations from text files.
        /// </summary>
        /// <param name="confidences">The confidence values.</param>
        /// <returns>The usability scores for every segment in the texts, and for the texts.</returns>
        public (List<TextSegmentUsability> usabilitySegments, List<TextUsability> usabilityTexts) EstimateQuality(
            IEnumerable<(MultiKeyRef key, double confidence)> confidences
        )
        {
            (List<TextSegmentScore> segmentScores, TextScores textScores) = ProjectChrF3(confidences);
            return ComputeSegmentUsability(segmentScores, textScores);
        }

        /// <summary>
        /// Estimate the quality of the pre-translations from USFM files.
        /// </summary>
        /// <param name="confidences">The confidence values.</param>
        /// <returns>The usability scores for every verse segment, chapter, and book.</returns>
        public (
            List<ScriptureSegmentUsability> usabilitySegments,
            List<ScriptureChapterUsability> usabilityChapters,
            List<ScriptureBookUsability> usabilityBooks
        ) EstimateQuality(IEnumerable<(ScriptureRef key, double confidence)> confidences)
        {
            (
                List<ScriptureSegmentScore> segmentScores,
                ScriptureChapterScores chapterScores,
                ScriptureBookScores bookScores
            ) = ProjectChrF3(confidences);
            return ComputeSegmentUsability(segmentScores, chapterScores, bookScores);
        }

        /// <summary>
        /// Calculates the geometric mean for a collection of values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns>The geometric mean.</returns>
        private static double GeometricMean(IList<double> values)
        {
            // Geometric mean requires positive values
            if (values == null || !values.Any() || values.Any(x => x <= 0))
                return 0;

            // Compute the sum of the natural logarithms of all values,
            // and divide by the count of numbers and take the exponential
            return Math.Exp(values.Sum(Math.Log) / values.Count);
        }

        private double CalculateUsableProbability(double chrF3)
        {
            double usableWeight = Math.Exp(-Math.Pow(chrF3 - Usable.Mean, 2) / (2 * Usable.Variance)) * Usable.Count;
            double unusableWeight =
                Math.Exp(-Math.Pow(chrF3 - Unusable.Mean, 2) / (2 * Unusable.Variance)) * Unusable.Count;
            return usableWeight / (usableWeight + unusableWeight);
        }

        private List<ScriptureBookUsability> ComputeBookUsability(ScriptureBookScores bookScores)
        {
            var usabilityBooks = new List<ScriptureBookUsability>();
            foreach (string book in bookScores.Scores.Keys)
            {
                Score score = bookScores.GetScore(book);
                if (score is null)
                    continue;

                List<double> bookUsabilities = bookScores.GetSegmentUsabilities(book);
                double averageProbability = bookUsabilities.Average();
                usabilityBooks.Add(
                    new ScriptureBookUsability(
                        book,
                        label: BookThresholds.ReturnLabel(averageProbability),
                        usability: averageProbability,
                        projectedChrF3: score.ProjectedChrF3
                    )
                );
            }

            return usabilityBooks;
        }

        private List<ScriptureChapterUsability> ComputeChapterUsability(ScriptureChapterScores chapterScores)
        {
            var usabilityChapters = new List<ScriptureChapterUsability>();
            foreach (KeyValuePair<string, Dictionary<int, Score>> chapterScoresByBook in chapterScores.Scores)
            {
                string book = chapterScoresByBook.Key;
                foreach (int chapter in chapterScoresByBook.Value.Keys)
                {
                    Score score = chapterScores.GetScore(book, chapter);
                    if (score is null)
                        continue;

                    List<double> chapterUsabilities = chapterScores.GetSegmentUsabilities(book, chapter);
                    double averageProbability = chapterUsabilities.Average();
                    usabilityChapters.Add(
                        new ScriptureChapterUsability(
                            book,
                            chapter,
                            label: ChapterThresholds.ReturnLabel(averageProbability),
                            usability: averageProbability,
                            projectedChrF3: score.ProjectedChrF3
                        )
                    );
                }
            }

            return usabilityChapters;
        }

        private (
            List<ScriptureSegmentUsability>,
            List<ScriptureChapterUsability>,
            List<ScriptureBookUsability>
        ) ComputeSegmentUsability(
            List<ScriptureSegmentScore> segmentScores,
            ScriptureChapterScores chapterScores,
            ScriptureBookScores bookScores
        )
        {
            var usabilitySegments = new List<ScriptureSegmentUsability>();
            foreach (ScriptureSegmentScore segmentScore in segmentScores)
            {
                double probability = CalculateUsableProbability(segmentScore.ProjectedChrF3);
                chapterScores.AppendSegmentUsability(
                    segmentScore.ScriptureRef.Book,
                    segmentScore.ScriptureRef.ChapterNum,
                    probability
                );
                bookScores.AppendSegmentUsability(segmentScore.ScriptureRef.Book, probability);
                usabilitySegments.Add(
                    new ScriptureSegmentUsability(
                        scriptureRef: segmentScore.ScriptureRef,
                        label: SegmentThresholds.ReturnLabel(probability),
                        usability: probability,
                        projectedChrF3: segmentScore.ProjectedChrF3
                    )
                );
            }

            return (usabilitySegments, ComputeChapterUsability(chapterScores), ComputeBookUsability(bookScores));
        }

        private (List<TextSegmentUsability>, List<TextUsability>) ComputeSegmentUsability(
            List<TextSegmentScore> segmentScores,
            TextScores textScores
        )
        {
            var usabilitySegments = new List<TextSegmentUsability>();
            foreach (TextSegmentScore segmentScore in segmentScores)
            {
                double probability = CalculateUsableProbability(segmentScore.ProjectedChrF3);
                textScores.AppendSegmentUsability(segmentScore.TextId, probability);
                usabilitySegments.Add(
                    new TextSegmentUsability(
                        segmentRef: segmentScore.SegmentRef,
                        label: SegmentThresholds.ReturnLabel(probability),
                        usability: probability,
                        projectedChrF3: segmentScore.ProjectedChrF3
                    )
                );
            }

            return (usabilitySegments, ComputeTextUsability(textScores));
        }

        private List<TextUsability> ComputeTextUsability(TextScores textScores)
        {
            var usabilityTexts = new List<TextUsability>();
            foreach (string textId in textScores.Scores.Keys)
            {
                Score score = textScores.GetScore(textId);
                if (score is null)
                    continue;

                List<double> textUsabilities = textScores.GetSegmentUsabilities(textId);
                double averageProbability = textUsabilities.Average();
                usabilityTexts.Add(
                    new TextUsability(
                        textId,
                        label: BookThresholds.ReturnLabel(averageProbability),
                        usability: averageProbability,
                        projectedChrF3: score.ProjectedChrF3
                    )
                );
            }

            return usabilityTexts;
        }

        private (List<TextSegmentScore> segmentScores, TextScores textScores) ProjectChrF3(
            IEnumerable<(MultiKeyRef, double)> confidences
        )
        {
            var confidencesByTextId = new Dictionary<string, List<double>>();
            var segmentScores = new List<TextSegmentScore>();
            foreach ((MultiKeyRef segmentRef, double confidence) in confidences)
            {
                var score = new TextSegmentScore(_slope, confidence, _intercept, segmentRef);
                segmentScores.Add(score);

                // Record the confidence by text id
                string textId = segmentRef.TextId;
                if (!confidencesByTextId.TryGetValue(textId, out List<double> textConfidences))
                {
                    textConfidences = new List<double>();
                    confidencesByTextId[textId] = textConfidences;
                }

                textConfidences.Add(confidence);
            }

            var textScores = new TextScores();
            foreach (KeyValuePair<string, List<double>> textIdConfidences in confidencesByTextId)
            {
                textScores.AddScore(
                    textIdConfidences.Key,
                    new Score(_slope, confidence: GeometricMean(textIdConfidences.Value), _intercept)
                );
            }

            return (segmentScores, textScores);
        }

        private (
            List<ScriptureSegmentScore> segmentScores,
            ScriptureChapterScores chapterScores,
            ScriptureBookScores bookScores
        ) ProjectChrF3(IEnumerable<(ScriptureRef, double)> confidences)
        {
            var confidencesByBook = new Dictionary<string, List<double>>();
            var confidencesByBookAndChapter = new Dictionary<(string, int), List<double>>();
            var segmentScores = new List<ScriptureSegmentScore>();
            foreach ((ScriptureRef scriptureRef, double confidence) in confidences)
            {
                var score = new ScriptureSegmentScore(_slope, confidence, _intercept, scriptureRef);
                segmentScores.Add(score);
                string book = scriptureRef.Book;
                int chapter = scriptureRef.ChapterNum;

                // Record the confidence by and chapter
                if (
                    !confidencesByBookAndChapter.TryGetValue(
                        (book, chapter),
                        out List<double> bookAndChapterConfidences
                    )
                )
                {
                    bookAndChapterConfidences = new List<double>();
                    confidencesByBookAndChapter[(book, chapter)] = bookAndChapterConfidences;
                }

                bookAndChapterConfidences.Add(confidence);

                // Record the confidence by book
                if (!confidencesByBook.TryGetValue(book, out List<double> bookConfidences))
                {
                    bookConfidences = new List<double>();
                    confidencesByBook[book] = bookConfidences;
                }

                bookConfidences.Add(confidence);
            }

            var chapterScores = new ScriptureChapterScores();
            foreach (
                KeyValuePair<
                    (string Book, int Chapter),
                    List<double>
                > bookAndChapterConfidences in confidencesByBookAndChapter
            )
            {
                chapterScores.AddScore(
                    bookAndChapterConfidences.Key.Book,
                    bookAndChapterConfidences.Key.Chapter,
                    new Score(_slope, confidence: GeometricMean(bookAndChapterConfidences.Value), _intercept)
                );
            }

            var bookScores = new ScriptureBookScores();
            foreach (KeyValuePair<string, List<double>> bookConfidences in confidencesByBook)
            {
                bookScores.AddScore(
                    bookConfidences.Key,
                    new Score(_slope, confidence: GeometricMean(bookConfidences.Value), _intercept)
                );
            }

            return (segmentScores, chapterScores, bookScores);
        }
    }
}
