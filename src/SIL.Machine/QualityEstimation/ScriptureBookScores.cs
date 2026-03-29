using System.Collections.Generic;

namespace SIL.Machine.QualityEstimation
{
    internal class ScriptureBookScores
    {
        private readonly Dictionary<string, List<double>> _segmentUsabilities = new Dictionary<string, List<double>>();

        public readonly Dictionary<string, Score> Scores = new Dictionary<string, Score>();

        public void AddScore(string book, Score score) => Scores[book] = score;

        public Score GetScore(string book) => Scores.TryGetValue(book, out Score score) ? score : null;

        public void AppendSegmentUsability(string book, double usability)
        {
            if (!_segmentUsabilities.TryGetValue(book, out List<double> list))
            {
                list = new List<double>();
                _segmentUsabilities[book] = list;
            }

            list.Add(usability);
        }

        public List<double> GetSegmentUsabilities(string book) =>
            _segmentUsabilities.TryGetValue(book, out List<double> list) ? new List<double>(list) : new List<double>();
    }
}
