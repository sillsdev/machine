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

        /// <summary>
        /// Number of independent Gibbs chains trained in parallel. Marginals are summed across
        /// chains at decode time (eflomal's n_samplers scheme). Default 3.
        /// </summary>
        public int? EflomalNumSamplers { get; set; }

        /// <summary>
        /// Trains the Gibbs sampler chains serially rather than across threads, so a fixed seed
        /// produces a reproducible model at the cost of parallelism. Default false.
        /// </summary>
        public bool? EflomalDeterministic { get; set; }

        /// <summary>
        /// Random seed for the Gibbs samplers. Chain <c>s</c> is seeded with <c>seed + s * 2654435761</c>.
        /// Combine with <see cref="EflomalDeterministic"/> for fully reproducible training. Default 1351155463.
        /// </summary>
        public uint? EflomalSeed { get; set; }

        /// <summary>
        /// When true, the lexical model uses the plain denominator <c>1/N(e)</c> instead of the
        /// Dirichlet-smoothed <c>1/(N(e) + alphaLex * |V|)</c>. Default true.
        /// </summary>
        public bool? EflomalLexNorm { get; set; }

        /// <summary>Lexical Dirichlet prior for source words. Matches eflomal LEX_ALPHA. Default 0.001.</summary>
        public double? EflomalLexAlpha { get; set; }

        /// <summary>Jump distribution Dirichlet prior. Matches eflomal JUMP_ALPHA. Default 0.5.</summary>
        public double? EflomalJumpAlpha { get; set; }

        /// <summary>Fertility distribution Dirichlet prior. Matches eflomal FERT_ALPHA. Default 0.5.</summary>
        public double? EflomalFertilityAlpha { get; set; }

        /// <summary>
        /// Fixed probability of aligning a target token to the NULL source word (IBM1 mixing weight).
        /// Not a Dirichlet prior; controls the null/non-null split before lexical sampling. Default 0.2.
        /// </summary>
        public double? EflomalP0 { get; set; }

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

        // Eflomal runs its IBM1->HMM->fertility cascade internally as a single model, reusing the
        // IBM1/HMM/IBM3 iteration counts for its three stages (IBM3 drives the fertility stage).
        // When none of them are specified, the model derives the schedule automatically from the
        // corpus size, so no explicit schedule should be set. When only some are specified, the
        // rest fall back to the Thot model's per-stage defaults (DefaultIbm1Iters/DefaultHmmIters/
        // DefaultFertilityIters).
        private const int DefaultEflomalIbm1IterationCount = 8;
        private const int DefaultEflomalHmmIterationCount = 8;
        private const int DefaultEflomalFertilityIterationCount = 32;

        public bool IsEflomalScheduleSpecified =>
            Ibm1IterationCount.HasValue || HmmIterationCount.HasValue || Ibm3IterationCount.HasValue;

        public int GetEflomalIbm1IterationCount() => Ibm1IterationCount ?? DefaultEflomalIbm1IterationCount;

        public int GetEflomalHmmIterationCount() => HmmIterationCount ?? DefaultEflomalHmmIterationCount;

        public int GetEflomalFertilityIterationCount() => Ibm3IterationCount ?? DefaultEflomalFertilityIterationCount;

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
