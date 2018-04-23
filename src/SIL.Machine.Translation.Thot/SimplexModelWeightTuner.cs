using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Optimization;
using SIL.Extensions;

namespace SIL.Machine.Translation.Thot
{
	public class SimplexModelWeightTuner : IParameterTuner
	{
		public SimplexModelWeightTuner()
		{
			ConvergenceTolerance = 0.001;
			MaxFunctionEvaluations = 100;
		}

		public double ConvergenceTolerance { get; set; }
		public int MaxFunctionEvaluations { get; set; }
		public int ProgressIncrementInterval { get; set; }

		public ThotSmtParameters Tune(ThotSmtParameters parameters,
			IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus, ThotTrainProgressReporter reporter,
			SmtBatchTrainStats stats)
		{
			float sentLenWeight = parameters.ModelWeights[7];
			int numFuncEvals = 0;
			double Evaluate(Vector weights)
			{
				ThotSmtParameters newParameters = parameters.Clone();
				newParameters.ModelWeights = weights.Select(w => (float) w).Concat(sentLenWeight).ToArray();
				newParameters.Freeze();
				double quality = CalculateBleu(newParameters, tuneSourceCorpus, tuneTargetCorpus);
				numFuncEvals++;
				if (numFuncEvals < MaxFunctionEvaluations && ProgressIncrementInterval > 0
					&& numFuncEvals % ProgressIncrementInterval == 0)
				{
					reporter.Step();
				}
				else
				{
					reporter.CheckCanceled();
				}
				return quality;
			};
			var simplex = new NelderMeadSimplex(ConvergenceTolerance, MaxFunctionEvaluations, 1.0);
			MinimizationResult result = simplex.FindMinimum(Evaluate,
				parameters.ModelWeights.Select(w => (double) w).Take(7));

			stats.TranslationModelBleu = 1.0 - result.ErrorValue;

			ThotSmtParameters bestParameters = parameters.Clone();
			bestParameters.ModelWeights = result.MinimizingPoint.Select(w => (float) w).Concat(sentLenWeight).ToArray();
			bestParameters.Freeze();
			return bestParameters;
		}

		private static double CalculateBleu(ThotSmtParameters parameters,
			IReadOnlyList<IReadOnlyList<string>> sourceCorpus, IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus)
		{
			IEnumerable<IReadOnlyList<string>> translations = GenerateTranslations(parameters, sourceCorpus);
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

		private static IEnumerable<IReadOnlyList<string>> GenerateTranslations(ThotSmtParameters parameters,
			IReadOnlyList<IReadOnlyList<string>> sourceCorpus)
		{
			var results = new IReadOnlyList<string>[sourceCorpus.Count];
			Parallel.ForEach(Partitioner.Create(0, sourceCorpus.Count), range =>
				{
					IntPtr smtModelHandle = IntPtr.Zero, decoderHandle = IntPtr.Zero;
					try
					{
						smtModelHandle = Thot.LoadSmtModel(parameters);
						decoderHandle = Thot.LoadDecoder(smtModelHandle, parameters);
						for (int i = range.Item1; i < range.Item2; i++)
						{
							IReadOnlyList<string> segment = sourceCorpus[i];
							results[i] = Thot.DoTranslate(decoderHandle, Thot.decoder_translate, segment, false,
								segment, (s, t, d) => t);
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
