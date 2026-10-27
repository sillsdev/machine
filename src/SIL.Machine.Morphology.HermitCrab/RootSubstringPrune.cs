using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal enum RootSubstringPruneMode
    {
        Off,
        Shadow,
        On,
    }

    /// <summary>
    /// Decides, once per stratum, whether an analysis rule output that contains no root as a contiguous run of
    /// segments can be dropped. Under the three conditions below, every word analysis can derive below such an
    /// output is a contiguous run of that output's segment nodes, with feature structures at least as specific,
    /// so an exact root match anywhere below it would be a substring match in the output itself:
    /// 1. every morphological, template-slot and compounding rule of the stratum and of each deeper stratum
    ///    writes inserted material, then every LHS part copied unmodified, once, in LHS order and with nothing
    ///    between the copies, then inserted material (a compounding rule only needs this for its head parts);
    /// 2. no deeper stratum has phonological rules (the stratum's own are unapplied before any rule output);
    /// 3. the language has no lexical-pattern roots, which <see cref="Morpher.ParseWord(string, out object, bool)"/>
    ///    matches against analyses when guessing roots.
    /// </summary>
    internal sealed class RootSubstringPruneStratum
    {
        public RootSubstringPruneStratum(IReadOnlyList<RootAllomorphTrie> tries, string disabledReason)
        {
            Tries = tries;
            DisabledReason = disabledReason;
        }

        public IReadOnlyList<RootAllomorphTrie> Tries { get; }

        /// <summary>The first rule or condition that disables the prune, or null if it is enabled.</summary>
        public string DisabledReason { get; }

        public bool Enabled => DisabledReason == null;

        public static Dictionary<Stratum, RootSubstringPruneStratum> Compute(
            Language language,
            IReadOnlyDictionary<Stratum, RootAllomorphTrie> tries,
            bool hasLexicalPatterns
        )
        {
            var result = new Dictionary<Stratum, RootSubstringPruneStratum>();
            string firstReason = hasLexicalPatterns ? "the language has lexical-pattern roots" : null;
            var coveredTries = new List<RootAllomorphTrie>();
            string pruleReason = null;
            string ruleReason = null;
            foreach (Stratum stratum in language.Strata)
            {
                // the phonological rules of the strata below this one run when analysis enters them
                string reason = firstReason ?? pruleReason;
                if (ruleReason == null)
                    ruleReason = FirstNonContiguousRemovingRule(stratum);
                reason = reason ?? ruleReason;
                if (!tries[stratum].IsEmpty)
                    coveredTries.Add(tries[stratum]);
                result[stratum] = new RootSubstringPruneStratum(coveredTries.ToArray(), reason);
                if (pruleReason == null && stratum.PhonologicalRules.Count > 0)
                    pruleReason = $"deeper stratum '{stratum.Name}' has phonological rules";
            }
            return result;
        }

        private static string FirstNonContiguousRemovingRule(Stratum stratum)
        {
            foreach (IMorphologicalRule rule in stratum.MorphologicalRules)
            {
                string reason = NonContiguousRemovingReason(rule);
                if (reason != null)
                    return $"rule '{rule.Name}' of stratum '{stratum.Name}' {reason}";
            }
            foreach (
                MorphemicMorphologicalRule rule in stratum
                    .AffixTemplates.SelectMany(t => t.Slots)
                    .SelectMany(s => s.Rules)
                    .Distinct()
            )
            {
                string reason = NonContiguousRemovingReason(rule);
                if (reason != null)
                    return $"template rule '{rule.Name}' of stratum '{stratum.Name}' {reason}";
            }
            return null;
        }

        internal static string NonContiguousRemovingReason(IMorphologicalRule rule)
        {
            switch (rule)
            {
                case AffixProcessRule affixRule:
                    return AllomorphsReason(affixRule.Allomorphs);
                case RealizationalAffixProcessRule realizationalRule:
                    return AllomorphsReason(realizationalRule.Allomorphs);
                case CompoundingRule compoundingRule:
                    foreach (CompoundingSubrule subrule in compoundingRule.Subrules)
                    {
                        string reason = BlockReason(
                            subrule.HeadLhs,
                            subrule.Rhs,
                            new HashSet<string>(subrule.HeadLhs.Select(p => p.Name))
                        );
                        if (reason != null)
                            return reason;
                    }
                    return null;
                default:
                    return "has an unknown rule type";
            }
        }

        private static string AllomorphsReason(IEnumerable<AffixProcessAllomorph> allomorphs)
        {
            foreach (AffixProcessAllomorph allomorph in allomorphs)
            {
                string reason = BlockReason(
                    allomorph.Lhs,
                    allomorph.Rhs,
                    new HashSet<string>(allomorph.Lhs.Select(p => p.Name))
                );
                if (reason != null)
                    return reason;
            }
            return null;
        }

        private static string BlockReason(
            IList<Pattern<Word, ShapeNode>> lhs,
            IList<MorphologicalOutputAction> rhs,
            HashSet<string> outputParts
        )
        {
            var copies = new List<string>();
            int firstCopy = -1;
            int lastCopy = -1;
            for (int i = 0; i < rhs.Count; i++)
            {
                switch (rhs[i])
                {
                    case InsertSegments _:
                    case InsertSimpleContext _:
                        break;
                    case CopyFromInput copy:
                        if (!outputParts.Contains(copy.PartName))
                            break;
                        if (copies.Contains(copy.PartName))
                            return "copies a part more than once";
                        copies.Add(copy.PartName);
                        if (firstCopy < 0)
                            firstCopy = i;
                        lastCopy = i;
                        break;
                    case ModifyFromInput _:
                        return "modifies a part";
                    default:
                        return "has an unknown output action";
                }
            }

            if (copies.Count > 0 && lastCopy - firstCopy + 1 != copies.Count)
                return "inserts or copies other material between its output parts";

            int next = 0;
            foreach (Pattern<Word, ShapeNode> part in lhs)
            {
                if (next < copies.Count && copies[next] == part.Name)
                    next++;
                else if (copies.Contains(part.Name))
                    return "copies its parts out of LHS order";
                else if (HasSegments(part))
                    return $"drops the part '{part.Name}', which can hold segments, so analysis restores them";
            }
            return null;
        }

        private static bool HasSegments(PatternNode<Word, ShapeNode> node)
        {
            if (node is Constraint<Word, ShapeNode> constraint)
                return constraint.Type() == HCFeatureSystem.Segment;
            return !node.IsLeaf && node.Children.Any(HasSegments);
        }
    }
}
