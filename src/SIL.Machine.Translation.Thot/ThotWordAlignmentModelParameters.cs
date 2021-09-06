using System.Collections.Generic;

namespace SIL.Machine.Translation.Thot
{
	public class ThotWordAlignmentModelParameters
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
			if (modelType < ThotWordAlignmentModelType.Ibm1)
				return 0;

			return Ibm1IterationCount ?? 5;
		}

		public int GetIbm2IterationCount(ThotWordAlignmentModelType modelType)
		{
			if (modelType < ThotWordAlignmentModelType.Ibm2)
				return 0;

			if (modelType == ThotWordAlignmentModelType.Ibm2)
				return Ibm2IterationCount ?? 5;

			return Ibm2IterationCount ?? 0;
		}

		public int GetHmmIterationCount(ThotWordAlignmentModelType modelType)
		{
			if (modelType < ThotWordAlignmentModelType.Hmm || Ibm2IterationCount > 0)
				return 0;

			return HmmIterationCount ?? 5;
		}

		public int GetIbm3IterationCount(ThotWordAlignmentModelType modelType)
		{
			if (modelType < ThotWordAlignmentModelType.Ibm3)
				return 0;

			return Ibm3IterationCount ?? 5;
		}

		public int GetIbm4IterationCount(ThotWordAlignmentModelType modelType)
		{
			if (modelType < ThotWordAlignmentModelType.Ibm4)
				return 0;

			return Ibm4IterationCount ?? 5;
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
	}
}
