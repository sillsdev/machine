using System.Collections.Generic;
using CaseExtensions;

namespace SIL.Machine.Translation
{
    public enum SymmetrizationHeuristic
    {
        None,

        /// <summary>
        /// matrix union
        /// </summary>
        Union,

        /// <summary>
        /// matrix intersection
        /// </summary>
        Intersection,

        /// <summary>
        /// method in "Improved Alignment Models for Statistical Machine Translation" (Och et al., 1999).
        /// </summary>
        Och,

        /// <summary>
        /// "base" method in "Statistical Phrase-Based Translation" (Koehn et al., 2003) without final step.
        /// </summary>
        Grow,

        /// <summary>
        /// "diag" method in "Statistical Phrase-Based Translation" (Koehn et al., 2003) without final step.
        /// </summary>
        GrowDiag,

        /// <summary>
        /// "diag" method in "Statistical Phrase-Based Translation" (Koehn et al., 2003).
        /// </summary>
        GrowDiagFinal,

        /// <summary>
        /// "diag-and" method in "Statistical Phrase-Based Translation" (Koehn et al., 2003).
        /// </summary>
        GrowDiagFinalAnd
    }

    public static class SymmetrizationHelpers
    {
        public const string Och = "och";
        public const string Union = "union";
        public const string Intersection = "intersection";
        public const string Grow = "grow";
        public const string GrowDiag = "grow-diag";
        public const string GrowDiagFinal = "grow-diag-final";
        public const string GrowDiagFinalAnd = "grow-diag-final-and";
        public const string None = "none";

        public static bool ValidateSymmetrizationHeuristicOption(string value, bool noneAllowed = true)
        {
            var validHeuristics = new HashSet<string>
            {
                Och,
                Union,
                Intersection,
                Grow,
                GrowDiag,
                GrowDiagFinal,
                GrowDiagFinalAnd
            };
            if (noneAllowed)
                validHeuristics.Add(None);
            return string.IsNullOrEmpty(value) || validHeuristics.Contains(value.ToLowerInvariant());
        }

        public static SymmetrizationHeuristic GetSymmetrizationHeuristic(string value)
        {
            switch (value.ToKebabCase())
            {
                case None:
                    return SymmetrizationHeuristic.None;
                case Union:
                    return SymmetrizationHeuristic.Union;
                case Intersection:
                    return SymmetrizationHeuristic.Intersection;
                case Grow:
                    return SymmetrizationHeuristic.Grow;
                case GrowDiag:
                    return SymmetrizationHeuristic.GrowDiag;
                case GrowDiagFinal:
                    return SymmetrizationHeuristic.GrowDiagFinal;
                case GrowDiagFinalAnd:
                    return SymmetrizationHeuristic.GrowDiagFinalAnd;
                default:
                    return SymmetrizationHeuristic.Och;
            }
        }
    }
}
