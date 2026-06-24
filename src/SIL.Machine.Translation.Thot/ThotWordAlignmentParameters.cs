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
        public int? EflomalIterationCount { get; set; }
        public int? EflomalIbm1IterationCount { get; set; }
        public int? EflomalHmmIterationCount { get; set; }
        public int? EflomalFertilityIterationCount { get; set; }
        public int? EflomalNumSamplers { get; set; }

        /// <summary>Lexical Dirichlet prior for non-NULL source words. Matches eflomal LEX_ALPHA. Default 0.001.</summary>
        public double? EflomalLexAlpha { get; set; }

        /// <summary>
        /// Lexical Dirichlet prior for the NULL source word. Matches eflomal NULL_ALPHA. Default 0.001.
        /// Separate from <see cref="EflomalLexAlpha"/> so the null word's smoothing can be tuned
        /// independently from the regular source vocabulary.
        /// </summary>
        public double? EflomalNullAlpha { get; set; }

        /// <summary>Jump distribution Dirichlet prior. Matches eflomal JUMP_ALPHA. Default 0.5.</summary>
        public double? EflomalJumpAlpha { get; set; }

        /// <summary>Fertility distribution Dirichlet prior. Matches eflomal FERT_ALPHA. Default 0.5.</summary>
        public double? EflomalFertilityAlpha { get; set; }

        /// <summary>
        /// Fixed probability of aligning a target token to the NULL source word (IBM1 mixing weight).
        /// Not a Dirichlet prior; controls the null/non-null split before lexical sampling. Default 0.2.
        /// </summary>
        public double? EflomalNullProb { get; set; }

        /// <summary>
        /// Half-width of the jump distribution window. Offsets beyond ±JumpWindow are clamped.
        /// Roughly corresponds to eflomal JUMP_MAX_EST. Default 100.
        /// </summary>
        public int? EflomalJumpWindow { get; set; }
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
            return GetIbmIterationCount(
                modelType,
                ThotWordAlignmentModelType.Ibm2,
                Ibm2IterationCount,
                defaultInitIterationCount: 0
            );
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

        // Eflomal runs its IBM1->HMM->fertility cascade internally, so the trainer drives a single
        // model for the total number of sweeps.
        // When per-stage counts are set, the total is their sum; otherwise EflomalIterationCount
        // is used (default 12, matching the C++ model's default 4/4/4 schedule).
        public int GetEflomalIterationCount(ThotWordAlignmentModelType modelType)
        {
            if (modelType != ThotWordAlignmentModelType.Eflomal)
                return 0;
            if (EflomalIbm1IterationCount.HasValue || EflomalHmmIterationCount.HasValue || EflomalFertilityIterationCount.HasValue)
                return GetEflomalIbm1IterationCount() + GetEflomalHmmIterationCount() + GetEflomalFertilityIterationCount();
            return EflomalIterationCount ?? 12;
        }

        public int GetEflomalIbm1IterationCount() => EflomalIbm1IterationCount ?? 4;

        public int GetEflomalHmmIterationCount() => EflomalHmmIterationCount ?? 4;

        public int GetEflomalFertilityIterationCount() => EflomalFertilityIterationCount ?? 4;

        /// <summary>
        /// Number of independent Gibbs chains trained in parallel. Marginals are summed across
        /// chains at decode time (eflomal's n_samplers scheme). Default 1.
        /// </summary>
        public int GetEflomalNumSamplers(ThotWordAlignmentModelType modelType)
        {
            if (modelType == ThotWordAlignmentModelType.Eflomal)
                return EflomalNumSamplers ?? 1;
            return 1;
        }

        public bool GetVariationalBayes(ThotWordAlignmentModelType modelType)
        {
            if (VariationalBayes == null)
                return modelType == ThotWordAlignmentModelType.FastAlign;
            return (bool)VariationalBayes;
        }

        private static int GetIbmIterationCount(
            ThotWordAlignmentModelType modelType,
            ThotWordAlignmentModelType iterationCountModelType,
            int? iterationCount,
            int defaultInitIterationCount = 5
        )
        {
            if (modelType < iterationCountModelType)
                return 0;
            return iterationCount ?? (modelType == iterationCountModelType ? 4 : defaultInitIterationCount);
        }
    }
}
