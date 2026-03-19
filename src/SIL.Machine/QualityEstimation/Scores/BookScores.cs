using System.Collections.Generic;

namespace SIL.Machine.QualityEstimation.Scores
{
    public class BookScores
    {
        private readonly Dictionary<string, List<double>> _verseUsabilities = new Dictionary<string, List<double>>();

        public readonly Dictionary<string, Score> Scores = new Dictionary<string, Score>();

        public void AddScore(string book, Score score) => Scores[book] = score;

        public Score GetScore(string book) => Scores.TryGetValue(book, out Score score) ? score : null;

        public void AppendVerseUsability(string book, double usability)
        {
            if (!_verseUsabilities.TryGetValue(book, out List<double> list))
            {
                list = new List<double>();
                _verseUsabilities[book] = list;
            }

            list.Add(usability);
        }

        public List<double> GetVerseUsabilities(string book) =>
            _verseUsabilities.TryGetValue(book, out List<double> list) ? new List<double>(list) : new List<double>();
    }
}
