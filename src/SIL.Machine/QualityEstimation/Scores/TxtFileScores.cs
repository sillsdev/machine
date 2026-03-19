using System.Collections.Generic;

namespace SIL.Machine.QualityEstimation.Scores
{
    public class TxtFileScores
    {
        private readonly Dictionary<string, List<double>> _sequenceUsabilities = new Dictionary<string, List<double>>();

        public readonly Dictionary<string, Score> Scores = new Dictionary<string, Score>();

        public void AddScore(string targetDraftFileStem, Score score) => Scores[targetDraftFileStem] = score;

        public Score GetScore(string targetDraftFileStem) =>
            Scores.TryGetValue(targetDraftFileStem, out Score score) ? score : null;

        public void AppendSequenceUsability(string targetDraftFileStem, double usability)
        {
            if (!_sequenceUsabilities.TryGetValue(targetDraftFileStem, out List<double> list))
            {
                list = new List<double>();
                _sequenceUsabilities[targetDraftFileStem] = list;
            }

            list.Add(usability);
        }

        public List<double> GetSequenceUsabilities(string targetDraftFileStem) =>
            _sequenceUsabilities.TryGetValue(targetDraftFileStem, out List<double> list)
                ? new List<double>(list)
                : new List<double>();
    }
}
