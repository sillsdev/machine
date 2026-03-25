using System.Collections.Generic;

namespace SIL.Machine.QualityEstimation
{
    internal class ChapterScores
    {
        private readonly Dictionary<string, Dictionary<int, List<double>>> _verseUsabilities =
            new Dictionary<string, Dictionary<int, List<double>>>();

        public readonly Dictionary<string, Dictionary<int, Score>> Scores =
            new Dictionary<string, Dictionary<int, Score>>();

        public void AddScore(string book, int chapter, Score score)
        {
            if (!Scores.TryGetValue(book, out Dictionary<int, Score> chapters))
            {
                chapters = new Dictionary<int, Score>();
                Scores[book] = chapters;
            }

            chapters[chapter] = score;
        }

        public Score GetScore(string book, int chapter) =>
            Scores.TryGetValue(book, out Dictionary<int, Score> chapters)
            && chapters.TryGetValue(chapter, out Score score)
                ? score
                : null;

        public void AppendVerseUsability(string book, int chapter, double usability)
        {
            if (!_verseUsabilities.TryGetValue(book, out Dictionary<int, List<double>> chapters))
            {
                chapters = new Dictionary<int, List<double>>();
                _verseUsabilities[book] = chapters;
            }

            if (!chapters.TryGetValue(chapter, out List<double> list))
            {
                list = new List<double>();
                chapters[chapter] = list;
            }

            list.Add(usability);
        }

        public List<double> GetVerseUsabilities(string book, int chapter) =>
            _verseUsabilities.TryGetValue(book, out Dictionary<int, List<double>> chapters)
            && chapters.TryGetValue(chapter, out List<double> list)
                ? new List<double>(list)
                : new List<double>();
    }
}
