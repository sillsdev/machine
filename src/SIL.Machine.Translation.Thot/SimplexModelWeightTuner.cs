using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Optimization;
using SIL.Extensions;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation.Thot
{
	public class SimplexModelWeightTuner : IParameterTuner
	{
		private readonly ThotWordAlignmentModelType _wordAlignmentModelType;

		public SimplexModelWeightTuner(ThotWordAlignmentModelType wordAlignmentModelType)
		{
			_wordAlignmentModelType = wordAlignmentModelType;
		}

		public double ConvergenceTolerance { get; set; } = 0.001;
		public int MaxFunctionEvaluations { get; set; } = 100;
		public int MaxProgressFunctionEvaluations { get; set; } = 70;

		public ThotSmtParameters Tune(ThotSmtParameters parameters,
			IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus, TrainStats stats,
			IProgress<ProgressStatus> progress)
		{
			float sentLenWeight = parameters.ModelWeights[7];
			int numFuncEvals = 0;
			double Evaluate(Vector weights)
			{
				ThotSmtParameters newParameters = parameters.Clone();
				newParameters.ModelWeights = weights.Select(w => (float)w).Concat(sentLenWeight).ToArray();
				newParameters.Freeze();
				double quality = CalculateBleu(newParameters, tuneSourceCorpus, tuneTargetCorpus);
				numFuncEvals++;
				int currentStep = Math.Min(numFuncEvals, MaxProgressFunctionEvaluations);
				progress.Report(new ProgressStatus(currentStep, MaxProgressFunctionEvaluations));
				return quality;
			};
			progress.Report(new ProgressStatus(0, MaxFunctionEvaluations));
			var simplex = new NelderMeadSimplex(ConvergenceTolerance, MaxFunctionEvaluations, 1.0);
			MinimizationResult result = simplex.FindMinimum(Evaluate,
				parameters.ModelWeights.Select(w => (double)w).Take(7));

			stats.Metrics["bleu"] = 1.0 - result.ErrorValue;

			ThotSmtParameters bestParameters = parameters.Clone();
			bestParameters.ModelWeights = result.MinimizingPoint.Select(w => (float)w).Concat(sentLenWeight).ToArray();
			bestParameters.Freeze();

			if (result.FunctionEvaluationCount < MaxProgressFunctionEvaluations)
				progress.Report(new ProgressStatus(1.0));
			return bestParameters;
		}

		private double CalculateBleu(ThotSmtParameters parameters, IReadOnlyList<IReadOnlyList<string>> sourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus)
		{
			IEnumerable<IReadOnlyList<string>> translations = GenerateTranslations(parameters, sourceCorpus);
			double bleu = Evaluation.ComputeBleu(translations, tuneTargetCorpus);
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

		private IEnumerable<IReadOnlyList<string>> GenerateTranslations(ThotSmtParameters parameters,
			IReadOnlyList<IReadOnlyList<string>> sourceCorpus)
		{
			IntPtr smtModelHandle = IntPtr.Zero;
			try
			{
				smtModelHandle = Thot.LoadSmtModel(_wordAlignmentModelType, parameters);
				var results = new IReadOnlyList<string>[sourceCorpus.Count];
				Parallel.ForEach(Partitioner.Create(0, sourceCorpus.Count), range =>
					{
						IntPtr decoderHandle = IntPtr.Zero;
						try
						{
							decoderHandle = Thot.LoadDecoder(smtModelHandle, parameters);
							for (int i = range.Item1; i < range.Item2; i++)
							{
								IReadOnlyList<string> segment = sourceCorpus[i];
								results[i] = Thot.DoTranslate(decoderHandle, Thot.decoder_translate, segment,
									(s, t, d) => t);
							}
						}
						finally
						{
							if (decoderHandle != IntPtr.Zero)
								Thot.decoder_close(decoderHandle);
						}
					});
				return results;
			}
			finally
			{
				if (smtModelHandle != IntPtr.Zero)
					Thot.smtModel_close(smtModelHandle);
			}
		}
	}
}
