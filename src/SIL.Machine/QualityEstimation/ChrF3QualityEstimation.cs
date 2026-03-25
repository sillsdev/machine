using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;

namespace SIL.Machine.QualityEstimation
{
    /// <summary>
    /// Provides chrF3 quality estimation support for pre-translations.
    /// </summary>
    public class ChrF3QualityEstimation
    {
        private readonly BookScores _bookScores = new BookScores();
        private readonly ChapterScores _chapterScores = new ChapterScores();
        private readonly double _intercept;
        private readonly List<SequenceScore> _sequenceScores = new List<SequenceScore>();
        private readonly double _slope;
        private readonly TxtFileScores _txtFileScores = new TxtFileScores();
        private readonly List<VerseScore> _verseScores = new List<VerseScore>();

        public ChrF3QualityEstimation(double slope, double intercept)
        {
            _slope = slope;
            _intercept = intercept;
        }

        /// <summary>
        /// The threshold values used to calculate the usability label for every book.
        /// </summary>
        public Thresholds BookThresholds { get; set; } = new Thresholds(greenThreshold: 0.745, yellowThreshold: 0.62);

        /// <summary>
        /// The threshold values used to calculate the usability label for every chapter.
        /// </summary>
        public Thresholds ChapterThresholds { get; set; } =
            new Thresholds(greenThreshold: 0.745, yellowThreshold: 0.62);

        /// <summary>
        /// The threshold values used to calculate the usability label for every verse.
        /// </summary>
        public Thresholds VerseThresholds { get; set; } = new Thresholds(greenThreshold: 0.745, yellowThreshold: 0.62);

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
        /// <returns>The usability scores for every line in the text files, and for the text files.</returns>
        public (List<SequenceUsability> usabilitySequences, List<TxtFileUsability> usabilityTxtFiles) EstimateQuality(
            IEnumerable<(MultiKeyRef key, double confidence)> confidences
        )
        {
            ProjectChrF3(confidences);
            return ComputeSequenceUsability();
        }

        /// <summary>
        /// Estimate the quality of the pre-translations from USFM files.
        /// </summary>
        /// <param name="confidences">The confidence values.</param>
        /// <returns>The usability scores for every verse, chapter, and book.</returns>
        public (
            List<VerseUsability> usabilityVerses,
            List<ChapterUsability> usabilityChapters,
            List<BookUsability> usabilityBooks
        ) EstimateQuality(IEnumerable<(ScriptureRef key, double confidence)> confidences)
        {
            ProjectChrF3(confidences);
            return ComputeVerseUsability();
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

        private List<BookUsability> ComputeBookUsability()
        {
            var usabilityBooks = new List<BookUsability>();
            foreach (string book in _bookScores.Scores.Keys)
            {
                Score score = _bookScores.GetScore(book);
                if (score is null)
                    continue;

                List<double> bookUsabilities = _bookScores.GetVerseUsabilities(book);
                double averageProbability = bookUsabilities.Average();
                usabilityBooks.Add(
                    new BookUsability(
                        book,
                        label: BookThresholds.ReturnLabel(averageProbability),
                        usability: averageProbability,
                        projectedChrF3: score.ProjectedChrF3
                    )
                );
            }

            return usabilityBooks;
        }

        private List<ChapterUsability> ComputeChapterUsability()
        {
            var usabilityChapters = new List<ChapterUsability>();
            foreach (KeyValuePair<string, Dictionary<int, Score>> chapterScoresByBook in _chapterScores.Scores)
            {
                string book = chapterScoresByBook.Key;
                foreach (int chapter in chapterScoresByBook.Value.Keys)
                {
                    Score score = _chapterScores.GetScore(book, chapter);
                    if (score is null)
                        continue;

                    List<double> chapterUsabilities = _chapterScores.GetVerseUsabilities(book, chapter);
                    double averageProbability = chapterUsabilities.Average();
                    usabilityChapters.Add(
                        new ChapterUsability(
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

        private (List<SequenceUsability>, List<TxtFileUsability>) ComputeSequenceUsability()
        {
            var usabilitySequences = new List<SequenceUsability>();
            foreach (SequenceScore sequenceScore in _sequenceScores)
            {
                double probability = CalculateUsableProbability(sequenceScore.ProjectedChrF3);
                _txtFileScores.AppendSequenceUsability(sequenceScore.TargetDraftFileStem, probability);
                usabilitySequences.Add(
                    new SequenceUsability(
                        targetDraftFile: sequenceScore.TargetDraftFileStem,
                        sequenceNumber: sequenceScore.SequenceNumber,
                        label: VerseThresholds.ReturnLabel(probability),
                        usability: probability,
                        projectedChrF3: sequenceScore.ProjectedChrF3
                    )
                );
            }

            return (usabilitySequences, ComputeTxtFileUsability());
        }

        private List<TxtFileUsability> ComputeTxtFileUsability()
        {
            var usabilityTxtFiles = new List<TxtFileUsability>();
            foreach (string targetDraftFileStem in _txtFileScores.Scores.Keys)
            {
                Score score = _txtFileScores.GetScore(targetDraftFileStem);
                if (score is null)
                    continue;

                List<double> txtFileUsabilities = _txtFileScores.GetSequenceUsabilities(targetDraftFileStem);
                double averageProbability = txtFileUsabilities.Average();
                usabilityTxtFiles.Add(
                    new TxtFileUsability(
                        targetDraftFileStem,
                        label: BookThresholds.ReturnLabel(averageProbability),
                        usability: averageProbability,
                        projectedChrF3: score.ProjectedChrF3
                    )
                );
            }

            return usabilityTxtFiles;
        }

        private (List<VerseUsability>, List<ChapterUsability>, List<BookUsability>) ComputeVerseUsability()
        {
            var usabilityVerses = new List<VerseUsability>();
            foreach (VerseScore verseScore in _verseScores.Where(v => v.ScriptureRef.VerseNum > 0))
            {
                double probability = CalculateUsableProbability(verseScore.ProjectedChrF3);
                _chapterScores.AppendVerseUsability(
                    verseScore.ScriptureRef.Book,
                    verseScore.ScriptureRef.ChapterNum,
                    probability
                );
                _bookScores.AppendVerseUsability(verseScore.ScriptureRef.Book, probability);
                usabilityVerses.Add(
                    new VerseUsability(
                        book: verseScore.ScriptureRef.Book,
                        chapter: verseScore.ScriptureRef.ChapterNum,
                        verse: verseScore.ScriptureRef.Verse,
                        label: VerseThresholds.ReturnLabel(probability),
                        usability: probability,
                        projectedChrF3: verseScore.ProjectedChrF3
                    )
                );
            }

            return (usabilityVerses, ComputeChapterUsability(), ComputeBookUsability());
        }

        private void ProjectChrF3(IEnumerable<(MultiKeyRef, double)> confidences)
        {
            var confidencesByTxtFile = new Dictionary<string, List<double>>();
            foreach ((MultiKeyRef key, double confidence) in confidences)
            {
                if (key.Keys.Count >= 0 && int.TryParse(key.Keys[0].ToString(), out int sequenceNumber))
                {
                    string targetDraftFileStem = key.TextId;
                    var score = new SequenceScore(_slope, confidence, _intercept, sequenceNumber, targetDraftFileStem);
                    _sequenceScores.Add(score);

                    // Record the confidence by text file
                    if (!confidencesByTxtFile.TryGetValue(targetDraftFileStem, out List<double> txtFileConfidences))
                    {
                        txtFileConfidences = new List<double>();
                        confidencesByTxtFile[targetDraftFileStem] = txtFileConfidences;
                    }

                    txtFileConfidences.Add(confidence);
                }
            }

            foreach (KeyValuePair<string, List<double>> txtFileConfidences in confidencesByTxtFile)
            {
                _txtFileScores.AddScore(
                    txtFileConfidences.Key,
                    new Score(_slope, confidence: GeometricMean(txtFileConfidences.Value), _intercept)
                );
            }
        }

        private void ProjectChrF3(IEnumerable<(ScriptureRef, double)> confidences)
        {
            var confidencesByBook = new Dictionary<string, List<double>>();
            var confidencesByBookAndChapter = new Dictionary<(string, int), List<double>>();
            foreach ((ScriptureRef key, double confidence) in confidences)
            {
                var score = new VerseScore(_slope, confidence, _intercept, key);
                _verseScores.Add(score);
                string book = key.Book;
                int chapter = key.ChapterNum;

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

            foreach (KeyValuePair<string, List<double>> bookConfidences in confidencesByBook)
            {
                _bookScores.AddScore(
                    bookConfidences.Key,
                    new Score(_slope, confidence: GeometricMean(bookConfidences.Value), _intercept)
                );
            }

            foreach (
                KeyValuePair<
                    (string Book, int Chapter),
                    List<double>
                > bookAndChapterConfidences in confidencesByBookAndChapter
            )
            {
                _chapterScores.AddScore(
                    bookAndChapterConfidences.Key.Book,
                    bookAndChapterConfidences.Key.Chapter,
                    new Score(_slope, confidence: GeometricMean(bookAndChapterConfidences.Value), _intercept)
                );
            }
        }
    }
}
