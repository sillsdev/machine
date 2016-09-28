using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Optimization;
using SIL.Extensions;
using SIL.Progress;

namespace SIL.Machine.Translation.Thot
{
	public class SimplexLLWeightTuner : ILLWeightTuner
	{
		public double ProgressIncrement { get; set; }

		public double[] Tune(string cfgFileName, IList<IList<string>> tuneSourceCorpus, IList<IList<string>> tuneTargetCorpus, double[] initialWeights, IProgress progress = null)
		{
			double sentLenWeight = initialWeights[7];
			var simplex = new NelderMeadSimplex(0.001, 200, 1.0);
			MinimizationResult result = simplex.FindMinimum(w => CalculateBleu(cfgFileName, tuneSourceCorpus, tuneTargetCorpus, w, sentLenWeight),
				initialWeights.Take(7));
			return result.MinimizingPoint.Concat(sentLenWeight).ToArray();
		}

		private static double CalculateBleu(string tuneCfgFileName, IList<IList<string>> sourceCorpus, IList<IList<string>> tuneTargetCorpus, Vector weights,
			double sentLenWeight)
		{
			IntPtr decoderHandle = IntPtr.Zero, sessionHandle = IntPtr.Zero;
			try
			{
				decoderHandle = Thot.decoder_open(tuneCfgFileName);
				float[] weightArray = weights.Select(w => (float) w).Concat((float) sentLenWeight).ToArray();
				Thot.decoder_setLlWeights(decoderHandle, weightArray, (uint) weightArray.Length);
				sessionHandle = Thot.decoder_openSession(decoderHandle);
				double bleu = Evaluation.CalculateBleu(GenerateTranslations(sessionHandle, sourceCorpus), tuneTargetCorpus);
				double penalty = 0;
				for (int i = 0; i < weights.Count; i++)
				{
					if (i == 0 || i == 2)
						continue;

					if (weights[i] < 0)
						penalty += weights[i] * 1000 * -1;
				}
				return (1.0 - bleu) + penalty;
			}
			finally
			{
				if (sessionHandle != IntPtr.Zero)
					Thot.session_close(sessionHandle);
				if (decoderHandle != IntPtr.Zero)
					Thot.decoder_close(decoderHandle);
			}
		}

		private static IEnumerable<IList<string>> GenerateTranslations(IntPtr sessionHandle, IList<IList<string>> sourceCorpus)
		{
			foreach (IList<string> segment in sourceCorpus)
				yield return Thot.DoTranslate(sessionHandle, Thot.session_translate, segment, false, segment, (s, t, d) => t);
		}
	}
}
