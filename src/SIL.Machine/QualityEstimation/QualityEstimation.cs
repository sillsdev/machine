using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.QualityEstimation.Scores;
using SIL.Machine.QualityEstimation.Thresholds;
using SIL.Machine.QualityEstimation.Usability;
using SIL.Scripture;

namespace SIL.Machine.QualityEstimation
{
    public class QualityEstimation
    {
        public BookThresholds BookThresholds { get; set; } = new BookThresholds();

        public ChapterThresholds ChapterThresholds { get; set; } = new ChapterThresholds();

        public VerseThresholds VerseThresholds { get; set; } = new VerseThresholds();

        public UsabilityParameters Usable { get; set; } = UsabilityParameters.Usable;

        public UsabilityParameters Unusable { get; set; } = UsabilityParameters.Unusable;

        public List<BookUsability> UsabilityBooks { get; } = new List<BookUsability>();

        public List<ChapterUsability> UsabilityChapters { get; } = new List<ChapterUsability>();

        public List<SequenceUsability> UsabilitySequences { get; } = new List<SequenceUsability>();

        public List<TxtFileUsability> UsabilityTxtFiles { get; } = new List<TxtFileUsability>();

        public List<VerseUsability> UsabilityVerses { get; } = new List<VerseUsability>();

        public double CalculateUsableProbability(double chrF3)
        {
            double usableWeight = Math.Exp(-Math.Pow(chrF3 - Usable.Mean, 2) / (2 * Usable.Variance)) * Usable.Count;
            double unusableWeight =
                Math.Exp(-Math.Pow(chrF3 - Unusable.Mean, 2) / (2 * Unusable.Variance)) * Unusable.Count;
            return usableWeight / (usableWeight + unusableWeight);
        }

        public void ComputeBookUsability(BookScores bookScores)
        {
            foreach (string book in bookScores.Scores.Keys)
            {
                Score score = bookScores.GetScore(book);
                if (score is null)
                {
                    continue;
                }

                List<double> bookUsabilities = bookScores.GetVerseUsabilities(book);
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

        public void ComputeChapterUsability(ChapterScores chapterScores)
        {
            foreach (KeyValuePair<string, Dictionary<int, Score>> chapterScoresByBook in chapterScores.Scores)
            {
                string book = chapterScoresByBook.Key;
                foreach (int chapter in chapterScoresByBook.Value.Keys)
                {
                    Score score = chapterScores.GetScore(book, chapter);
                    if (score is null)
                    {
                        continue;
                    }

                    List<double> chapterUsabilities = chapterScores.GetVerseUsabilities(book, chapter);
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

        public void ComputeTxtFileUsability(TxtFileScores txtFileScores)
        {
            foreach (string targetDraftFileStem in txtFileScores.Scores.Keys)
            {
                Score score = txtFileScores.GetScore(targetDraftFileStem);
                if (score is null)
                {
                    continue;
                }

                List<double> txtFileUsabilities = txtFileScores.GetSequenceUsabilities(targetDraftFileStem);
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

        public void ComputeUsableProportions(
            List<VerseScore> verseScores,
            ref ChapterScores chapterScores,
            ref BookScores bookScores
        )
        {
            foreach (VerseScore verseScore in verseScores.Where(v => v.VerseRef.VerseNum > 0))
            {
                double probability = CalculateUsableProbability(verseScore.ProjectedChrF3);
                chapterScores.AppendVerseUsability(
                    verseScore.VerseRef.Book,
                    verseScore.VerseRef.ChapterNum,
                    probability
                );
                bookScores.AppendVerseUsability(verseScore.VerseRef.Book, probability);
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

            ComputeChapterUsability(chapterScores);
            ComputeBookUsability(bookScores);
        }

        public void ComputeUsableProportions(List<SequenceScore> sequenceScores, ref TxtFileScores txtFileScores)
        {
            foreach (SequenceScore sequenceScore in sequenceScores)
            {
                double probability = CalculateUsableProbability(sequenceScore.ProjectedChrF3);
                txtFileScores.AppendSequenceUsability(sequenceScore.TargetDraftFileStem, probability);
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

            ComputeTxtFileUsability(txtFileScores);
        }

        public void EstimateQuality(double slope, double intercept, Dictionary<string, double> confidences)
        {
            var sequenceScores = new List<SequenceScore>();
            var txtFileScores = new TxtFileScores();
            ProjectChrF3(slope, intercept, confidences, ref sequenceScores, ref txtFileScores);
            ComputeUsableProportions(sequenceScores, ref txtFileScores);
        }

        public void EstimateQuality(double slope, double intercept, Dictionary<VerseRef, double> confidences)
        {
            var verseScores = new List<VerseScore>();
            var chapterScores = new ChapterScores();
            var bookScores = new BookScores();
            ProjectChrF3(slope, intercept, confidences, ref verseScores, ref chapterScores, ref bookScores);
            ComputeUsableProportions(verseScores, ref chapterScores, ref bookScores);
        }

        public void ProjectChrF3(
            double slope,
            double intercept,
            Dictionary<string, double> confidences,
            ref List<SequenceScore> sequenceScores,
            ref TxtFileScores txtFileScores
        )
        {
            foreach (KeyValuePair<string, double> confidence in confidences)
            {
                string[] keyParts = confidence.Key.Split(':');
                if (keyParts.Length == 2 && int.TryParse(keyParts[1], out int sequenceNumber))
                {
                    string targetDraftFileStem = keyParts[0];
                    var score = new SequenceScore(
                        slope,
                        confidence.Value,
                        intercept,
                        sequenceNumber,
                        targetDraftFileStem
                    );
                    sequenceScores.Add(score);
                    txtFileScores.AddScore(targetDraftFileStem, score);
                }
            }
        }

        public void ProjectChrF3(
            double slope,
            double intercept,
            Dictionary<VerseRef, double> confidences,
            ref List<VerseScore> verseScores,
            ref ChapterScores chapterScores,
            ref BookScores bookScores
        )
        {
            foreach (KeyValuePair<VerseRef, double> confidence in confidences)
            {
                var score = new VerseScore(slope, confidence.Value, intercept, confidence.Key);
                verseScores.Add(score);
                string book = confidence.Key.Book;
                int chapter = confidence.Key.ChapterNum;
                chapterScores.AddScore(book, chapter, score);
                bookScores.AddScore(book, score);
            }
        }
    }
}
