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

		public IReadOnlyList<float> Tune(string tmFileNamePrefix, string lmFileNamePrefix, ThotSmtParameters parameters, IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus, IReadOnlyList<float> initialWeights, IProgress progress)
		{
			float sentLenWeight = initialWeights[7];
			int numFuncEvals = 0;
			Func<Vector, double> evalFunc = weights =>
			{
				ThotSmtParameters newParameters = parameters.Clone();
				newParameters.ModelWeights = weights.Select(w => (float) w).Concat(sentLenWeight).ToArray();
				newParameters.Freeze();
				double quality = CalculateBleu(tmFileNamePrefix, lmFileNamePrefix, newParameters, tuneSourceCorpus, tuneTargetCorpus);
				numFuncEvals++;
				if (ProgressIncrementInterval > 0 && numFuncEvals % ProgressIncrementInterval == 0)
					progress.ProgressIndicator.PercentCompleted += ProgressIncrement;
				return quality;
			};
			var simplex = new NelderMeadSimplex(ConvergenceTolerance, MaxFunctionEvaluations, 1.0) {IsCanceled = () => progress.CancelRequested};
			MinimizationResult result = simplex.FindMinimum(evalFunc, initialWeights.Select(w => (double) w).Take(7));
			return result.MinimizingPoint.Select(w => (float) w).Concat(sentLenWeight).ToArray();
		}

		private static double CalculateBleu(string tmFileNamePrefix, string lmFileNamePrefix, ThotSmtParameters parameters, IReadOnlyList<IReadOnlyList<string>> sourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus)
		{
			IEnumerable<IReadOnlyList<string>> translations = GenerateTranslations(tmFileNamePrefix, lmFileNamePrefix, parameters, sourceCorpus);
			double bleu = Evaluation.CalculateBleu(translations, tuneTargetCorpus);
			double penalty = 0;
			for (int i = 0; i < parameters.ModelWeights.Count; i++)
			{
				if (i == 0 || i == 2 || i == 7)
					continue;

				if (parameters.ModelWeights[i] < 0)
					penalty += parameters.ModelWeights[i] * 1000 * -1;
			}
			return (1.0 - bleu) + penalty;
		}

		private static IEnumerable<IReadOnlyList<string>> GenerateTranslations(string tmFileNamePrefix, string lmFileNamePrefix, ThotSmtParameters parameters,
			IReadOnlyList<IReadOnlyList<string>> sourceCorpus)
		{
			var results = new IReadOnlyList<string>[sourceCorpus.Count];
			Parallel.ForEach(Partitioner.Create(0, sourceCorpus.Count), range =>
				{
					IntPtr smtModelHandle = IntPtr.Zero, decoderHandle = IntPtr.Zero;
					try
					{
						smtModelHandle = Thot.LoadSmtModel(tmFileNamePrefix, lmFileNamePrefix, parameters);
						decoderHandle = Thot.LoadDecoder(smtModelHandle, parameters);
						for (int i = range.Item1; i < range.Item2; i++)
						{
							IReadOnlyList<string> segment = sourceCorpus[i];
							results[i] = Thot.DoTranslate(decoderHandle, Thot.decoder_translate, segment, false, segment, (s, t, d) => t);
						}
					}
					finally
					{
						if (decoderHandle != IntPtr.Zero)
							Thot.decoder_close(decoderHandle);
						if (smtModelHandle != IntPtr.Zero)
							Thot.smtModel_close(smtModelHandle);
					}
				});
			return results;
		}
	}
}
