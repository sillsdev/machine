using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.QualityEstimation.Scores;
using SIL.Machine.QualityEstimation.Usability;
using SIL.Scripture;

namespace SIL.Machine.QualityEstimation
{
    /// <summary>
    /// Provides chrF3 quality estimation support for pre-translations.
    /// </summary>
    public class QualityEstimation
    {
        private readonly BookScores _bookScores = new BookScores();
        private readonly ChapterScores _chapterScores = new ChapterScores();
        private readonly double _intercept;
        private readonly List<SequenceScore> _sequenceScores = new List<SequenceScore>();
        private readonly double _slope;
        private readonly TxtFileScores _txtFileScores = new TxtFileScores();
        private readonly List<VerseScore> _verseScores = new List<VerseScore>();

        public QualityEstimation(double slope, double intercept)
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
        /// The usability scores for every book.
        /// </summary>
        public List<BookUsability> UsabilityBooks { get; } = new List<BookUsability>();

        /// <summary>
        /// The usability scores for every chapter.
        /// </summary>
        public List<ChapterUsability> UsabilityChapters { get; } = new List<ChapterUsability>();

        /// <summary>
        /// The usability scores for every line in a text file.
        /// </summary>
        public List<SequenceUsability> UsabilitySequences { get; } = new List<SequenceUsability>();

        /// <summary>
        /// The usability scores for every text file.
        /// </summary>
        public List<TxtFileUsability> UsabilityTxtFiles { get; } = new List<TxtFileUsability>();

        /// <summary>
        /// The usability scores for every verse.
        /// </summary>
        public List<VerseUsability> UsabilityVerses { get; } = new List<VerseUsability>();

        /// <summary>
        /// Estimate the quality of the pre-translations from text files.
        /// </summary>
        /// <param name="confidences">The confidence values.</param>
        public void EstimateQuality(Dictionary<string, double> confidences)
        {
            ProjectChrF3(confidences);
            ComputeUsableProportionsForTxtFiles();
        }

        /// <summary>
        /// Estimate the quality of the pre-translations from USFM files.
        /// </summary>
        /// <param name="confidences">The confidence values.</param>
        public void EstimateQuality(Dictionary<VerseRef, double> confidences)
        {
            ProjectChrF3(confidences);
            ComputeUsableProportionsForVerses();
        }

        private double CalculateUsableProbability(double chrF3)
        {
            double usableWeight = Math.Exp(-Math.Pow(chrF3 - Usable.Mean, 2) / (2 * Usable.Variance)) * Usable.Count;
            double unusableWeight =
                Math.Exp(-Math.Pow(chrF3 - Unusable.Mean, 2) / (2 * Unusable.Variance)) * Unusable.Count;
            return usableWeight / (usableWeight + unusableWeight);
        }

        private void ComputeBookUsability()
        {
            foreach (string book in _bookScores.Scores.Keys)
            {
                Score score = _bookScores.GetScore(book);
                if (score is null)
                {
                    continue;
                }

                List<double> bookUsabilities = _bookScores.GetVerseUsabilities(book);
                double averageProbability = bookUsabilities.Average();
                UsabilityBooks.Add(
                    new BookUsability
                    {
                        Book = book,
                        Usability = averageProbability,
                        ProjectedChrF3 = score.ProjectedChrF3,
                        Label = BookThresholds.ReturnLabel(averageProbability),
                    }
                );
            }
        }

        public void ComputeChapterUsability()
        {
            foreach (KeyValuePair<string, Dictionary<int, Score>> chapterScoresByBook in _chapterScores.Scores)
            {
                string book = chapterScoresByBook.Key;
                foreach (int chapter in chapterScoresByBook.Value.Keys)
                {
                    Score score = _chapterScores.GetScore(book, chapter);
                    if (score is null)
                    {
                        continue;
                    }

                    List<double> chapterUsabilities = _chapterScores.GetVerseUsabilities(book, chapter);
                    double averageProbability = chapterUsabilities.Average();
                    UsabilityChapters.Add(
                        new ChapterUsability
                        {
                            Book = book,
                            Chapter = chapter,
                            Usability = averageProbability,
                            ProjectedChrF3 = score.ProjectedChrF3,
                            Label = ChapterThresholds.ReturnLabel(averageProbability),
                        }
                    );
                }
            }
        }

        private void ComputeTxtFileUsability()
        {
            foreach (string targetDraftFileStem in _txtFileScores.Scores.Keys)
            {
                Score score = _txtFileScores.GetScore(targetDraftFileStem);
                if (score is null)
                {
                    continue;
                }

                List<double> txtFileUsabilities = _txtFileScores.GetSequenceUsabilities(targetDraftFileStem);
                double averageProbability = txtFileUsabilities.Average();
                UsabilityTxtFiles.Add(
                    new TxtFileUsability
                    {
                        TargetDraftFile = targetDraftFileStem,
                        Usability = averageProbability,
                        ProjectedChrF3 = score.ProjectedChrF3,
                        Label = VerseThresholds.ReturnLabel(averageProbability),
                    }
                );
            }
        }

        private void ComputeUsableProportionsForVerses()
        {
            foreach (VerseScore verseScore in _verseScores.Where(v => v.VerseRef.VerseNum > 0))
            {
                double probability = CalculateUsableProbability(verseScore.ProjectedChrF3);
                _chapterScores.AppendVerseUsability(
                    verseScore.VerseRef.Book,
                    verseScore.VerseRef.ChapterNum,
                    probability
                );
                _bookScores.AppendVerseUsability(verseScore.VerseRef.Book, probability);
                UsabilityVerses.Add(
                    new VerseUsability
                    {
                        Book = verseScore.VerseRef.Book,
                        Chapter = verseScore.VerseRef.ChapterNum,
                        Verse = verseScore.VerseRef.Verse,
                        Usability = probability,
                        ProjectedChrF3 = verseScore.ProjectedChrF3,
                        Label = VerseThresholds.ReturnLabel(probability),
                    }
                );
            }

            ComputeChapterUsability();
            ComputeBookUsability();
        }

        private void ComputeUsableProportionsForTxtFiles()
        {
            foreach (SequenceScore sequenceScore in _sequenceScores)
            {
                double probability = CalculateUsableProbability(sequenceScore.ProjectedChrF3);
                _txtFileScores.AppendSequenceUsability(sequenceScore.TargetDraftFileStem, probability);
                UsabilitySequences.Add(
                    new SequenceUsability
                    {
                        TargetDraftFile = sequenceScore.TargetDraftFileStem,
                        SequenceNumber = sequenceScore.SequenceNumber,
                        Usability = probability,
                        ProjectedChrF3 = sequenceScore.ProjectedChrF3,
                        Label = VerseThresholds.ReturnLabel(probability),
                    }
                );
            }

            ComputeTxtFileUsability();
        }

        private void ProjectChrF3(Dictionary<string, double> confidences)
        {
            foreach (KeyValuePair<string, double> confidence in confidences)
            {
                string[] keyParts = confidence.Key.Split(':');
                if (keyParts.Length == 2 && int.TryParse(keyParts[1], out int sequenceNumber))
                {
                    string targetDraftFileStem = keyParts[0];
                    var score = new SequenceScore(
                        _slope,
                        confidence.Value,
                        _intercept,
                        sequenceNumber,
                        targetDraftFileStem
                    );
                    _sequenceScores.Add(score);
                    _txtFileScores.AddScore(targetDraftFileStem, score);
                }
            }
        }

        private void ProjectChrF3(Dictionary<VerseRef, double> confidences)
        {
            foreach (KeyValuePair<VerseRef, double> confidence in confidences)
            {
                var score = new VerseScore(_slope, confidence.Value, _intercept, confidence.Key);
                _verseScores.Add(score);
                string book = confidence.Key.Book;
                int chapter = confidence.Key.ChapterNum;
                _chapterScores.AddScore(book, chapter, score);
                _bookScores.AddScore(book, score);
            }
        }
    }
}
