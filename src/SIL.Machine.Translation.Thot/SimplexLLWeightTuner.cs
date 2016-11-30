using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Optimization;
using SIL.Extensions;
using SIL.Progress;

namespace SIL.Machine.Translation.Thot
{
	public class SimplexLLWeightTuner : ILLWeightTuner
	{
		public SimplexLLWeightTuner()
		{
			ConvergenceTolerance = 0.001;
			MaxFunctionEvaluations = 100;
		}

		public double ConvergenceTolerance { get; set; }
		public int MaxFunctionEvaluations { get; set; }
		public double ProgressIncrement { get; set; }
		public int ProgressIncrementInterval { get; set; }

		public IReadOnlyList<double> Tune(string cfgFileName, IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus, IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus,
			IReadOnlyList<double> initialWeights, IProgress progress)
		{
			double sentLenWeight = initialWeights[7];
			int numFuncEvals = 0;
			Func<Vector, double> evalFunc = w =>
			{
				double quality = CalculateBleu(cfgFileName, tuneSourceCorpus, tuneTargetCorpus, w, sentLenWeight);
				numFuncEvals++;
				if (ProgressIncrementInterval > 0 && numFuncEvals % ProgressIncrementInterval == 0)
					progress.ProgressIndicator.PercentCompleted += ProgressIncrement;
				return quality;
			};
			var simplex = new NelderMeadSimplex(ConvergenceTolerance, MaxFunctionEvaluations, 1.0) {IsCanceled = () => progress.CancelRequested};
			MinimizationResult result = simplex.FindMinimum(evalFunc, initialWeights.Take(7));
			return result.MinimizingPoint.Concat(sentLenWeight).ToArray();
		}

		private static double CalculateBleu(string tuneCfgFileName, IReadOnlyList<IReadOnlyList<string>> sourceCorpus, IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus,
			Vector weights, double sentLenWeight)
		{
			IEnumerable<IReadOnlyList<string>> translations = GenerateTranslations(tuneCfgFileName, sourceCorpus, weights, sentLenWeight);
			double bleu = Evaluation.CalculateBleu(translations, tuneTargetCorpus);
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

		private static IEnumerable<IReadOnlyList<string>> GenerateTranslations(string tuneCfgFileName, IReadOnlyList<IReadOnlyList<string>> sourceCorpus, IEnumerable<double> weights,
			double sentLenWeight)
		{
			float[] weightArray = weights.Select(w => (float) w).Concat((float) sentLenWeight).ToArray();
			var results = new IReadOnlyList<string>[sourceCorpus.Count];
			Parallel.ForEach(Partitioner.Create(0, sourceCorpus.Count), range =>
				{
					IntPtr decoderHandle = IntPtr.Zero, sessionHandle = IntPtr.Zero;
					try
					{
						decoderHandle = Thot.decoder_open(tuneCfgFileName);
						Thot.decoder_setLlWeights(decoderHandle, weightArray, (uint) weightArray.Length);
						sessionHandle = Thot.decoder_openSession(decoderHandle);
						for (int i = range.Item1; i < range.Item2; i++)
						{
							IReadOnlyList<string> segment = sourceCorpus[i];
							results[i] = Thot.DoTranslate(sessionHandle, Thot.session_translate, segment, false, segment, (s, t, d) => t);
						}
					}
					finally
					{
						if (sessionHandle != IntPtr.Zero)
							Thot.session_close(sessionHandle);
						if (decoderHandle != IntPtr.Zero)
							Thot.decoder_close(decoderHandle);
					}
				});
			return results;
		}
	}
}
