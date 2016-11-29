using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public class EcmScoreInfo
	{
		public IList<double> Scores { get; } = new List<double>();
		public IList<EditOperation> Operations { get; } = new List<EditOperation>();

		public void UpdatePositions(EcmScoreInfo prevEsi, IList<int> positions)
		{
			while (Scores.Count < prevEsi.Scores.Count)
				Scores.Add(0);

			while (Operations.Count < prevEsi.Operations.Count)
				Operations.Add(0);

			for (int i = 0; i < positions.Count; i++)
			{
				Scores[positions[i]] = prevEsi.Scores[positions[i]];
				if (prevEsi.Operations.Count > i)
					Operations[positions[i]] = prevEsi.Operations[positions[i]];
			}
		}

		public void RemoveLastPosition()
		{
			if (Scores.Count > 1)
				Scores.RemoveAt(Scores.Count - 1);
			if (Operations.Count > 1)
				Operations.RemoveAt(Operations.Count - 1);
		}
	}
}
