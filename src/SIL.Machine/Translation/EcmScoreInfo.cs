using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public class EcmScoreInfo
    {
        public List<double> Scores { get; } = new List<double>();
        public List<EditOperation> Operations { get; } = new List<EditOperation>();

        public void UpdatePositions(EcmScoreInfo prevEsi, List<int> positions)
        {
            while (Scores.Count < prevEsi.Scores.Count)
                Scores.Add(0);

            while (Operations.Count < prevEsi.Operations.Count)
                Operations.Add(EditOperation.None);

            for (int i = 0; i < positions.Count; i++)
            {
                Scores[positions[i]] = prevEsi.Scores[positions[i]];
                if (prevEsi.Operations.Count > i)
                    Operations[positions[i]] = prevEsi.Operations[positions[i]];
            }
        }

        public void RemoveLast()
        {
            if (Scores.Count > 1)
                Scores.RemoveAt(Scores.Count - 1);
            if (Operations.Count > 1)
                Operations.RemoveAt(Operations.Count - 1);
        }

        public int[] GetLastInsPrefixWordFromEsi()
        {
            var results = new int[Operations.Count];

            for (int j = Operations.Count - 1; j >= 0; j--)
            {
                switch (Operations[j])
                {
                    case EditOperation.Hit:
                        results[j] = j - 1;
                        break;

                    case EditOperation.Insert:
                        int tj = j;
                        while (tj >= 0 && Operations[tj] == EditOperation.Insert)
                            tj--;
                        if (Operations[tj] == EditOperation.Hit || Operations[tj] == EditOperation.Substitute)
                            tj--;
                        results[j] = tj;
                        break;

                    case EditOperation.Delete:
                        results[j] = j;
                        break;

                    case EditOperation.Substitute:
                        results[j] = j - 1;
                        break;

                    case EditOperation.None:
                        results[j] = 0;
                        break;
                }
            }

            return results;
        }
    }
}
