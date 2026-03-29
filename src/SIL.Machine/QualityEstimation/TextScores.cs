using System.Collections.Generic;

namespace SIL.Machine.QualityEstimation
{
    internal class TextScores
    {
        private readonly Dictionary<string, List<double>> _segmentUsabilities = new Dictionary<string, List<double>>();

        public readonly Dictionary<string, Score> Scores = new Dictionary<string, Score>();

        public void AddScore(string targetDraftFileStem, Score score) => Scores[targetDraftFileStem] = score;

        public Score GetScore(string targetDraftFileStem) =>
            Scores.TryGetValue(targetDraftFileStem, out Score score) ? score : null;

        public void AppendSegmentUsability(string textId, double usability)
        {
            if (!_segmentUsabilities.TryGetValue(textId, out List<double> list))
            {
                list = new List<double>();
                _segmentUsabilities[textId] = list;
            }

            list.Add(usability);
        }

        public List<double> GetSegmentUsabilities(string textId) =>
            _segmentUsabilities.TryGetValue(textId, out List<double> list)
                ? new List<double>(list)
                : new List<double>();
    }
}
