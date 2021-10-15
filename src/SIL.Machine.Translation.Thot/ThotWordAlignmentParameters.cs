using System.Collections.Generic;

namespace SIL.Machine.Translation.Thot
{
	public class ThotWordAlignmentParameters
	{
		public int? Ibm1IterationCount { get; set; }
		public int? Ibm2IterationCount { get; set; }
		public int? HmmIterationCount { get; set; }
		public int? Ibm3IterationCount { get; set; }
		public int? Ibm4IterationCount { get; set; }
		public int? FastAlignIterationCount { get; set; }
		public bool? VariationalBayes { get; set; }
		public double? FastAlignP0 { get; set; }
		public double? HmmP0 { get; set; }
		public double? HmmLexicalSmoothingFactor { get; set; }
		public double? HmmAlignmentSmoothingFactor { get; set; }
		public double? Ibm3FertilitySmoothingFactor { get; set; }
		public double? Ibm3CountThreshold { get; set; }
		public double? Ibm4DistortionSmoothingFactor { get; set; }
		public IReadOnlyDictionary<string, string> SourceWordClasses { get; set; } = new Dictionary<string, string>();
		public IReadOnlyDictionary<string, string> TargetWordClasses { get; set; } = new Dictionary<string, string>();

		public int GetIbm1IterationCount(ThotWordAlignmentModelType modelType)
		{
			return GetIbmIterationCount(modelType, ThotWordAlignmentModelType.Ibm1, Ibm1IterationCount);
		}

		public int GetIbm2IterationCount(ThotWordAlignmentModelType modelType)
		{
			return GetIbmIterationCount(modelType, ThotWordAlignmentModelType.Ibm2, Ibm2IterationCount,
				defaultInitIterationCount: 0);
		}

		public int GetHmmIterationCount(ThotWordAlignmentModelType modelType)
		{
			if (modelType != ThotWordAlignmentModelType.Hmm && Ibm2IterationCount > 0)
				return 0;

			return GetIbmIterationCount(modelType, ThotWordAlignmentModelType.Hmm, HmmIterationCount);
		}

		public int GetIbm3IterationCount(ThotWordAlignmentModelType modelType)
		{
			return GetIbmIterationCount(modelType, ThotWordAlignmentModelType.Ibm3, Ibm3IterationCount);
		}

		public int GetIbm4IterationCount(ThotWordAlignmentModelType modelType)
		{
			return GetIbmIterationCount(modelType, ThotWordAlignmentModelType.Ibm4, Ibm4IterationCount);
		}

		public int GetFastAlignIterationCount(ThotWordAlignmentModelType modelType)
		{
			if (modelType == ThotWordAlignmentModelType.FastAlign)
				return FastAlignIterationCount ?? 4;
			return 0;
		}

		public bool GetVariationalBayes(ThotWordAlignmentModelType modelType)
		{
			if (VariationalBayes == null)
				return modelType == ThotWordAlignmentModelType.FastAlign;
			return (bool)VariationalBayes;
		}

		private static int GetIbmIterationCount(ThotWordAlignmentModelType modelType,
			ThotWordAlignmentModelType iterationCountModelType, int? iterationCount, int defaultInitIterationCount = 5)
		{
			if (modelType < iterationCountModelType)
				return 0;
			return iterationCount ?? (modelType == iterationCountModelType ? 4 : defaultInitIterationCount);
		}
	}
}
