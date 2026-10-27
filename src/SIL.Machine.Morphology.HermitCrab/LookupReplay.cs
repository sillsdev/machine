using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal enum LookupReplayMode
    {
        Off,
        Shadow,
        On,
    }

    /// <summary>
    /// Decides from a lexical-lookup candidate's root and trail alone whether synthesis could ever apply every trail
    /// entry and end in a valid word. Returns false only when no synthesis exists, so rejecting never changes a
    /// parse. Candidates with a lexical family root are never rejected, because stem choice and blocking can replace
    /// the root.
    /// </summary>
    /// <remarks>
    /// The check is linear in the trail, stops at the first violated constraint, runs only when some rule carries a
    /// stem-name or MPR constraint, and keeps nothing between candidates.
    /// </remarks>
    internal sealed class LookupReplay
    {
        private readonly bool _checksMpr;
        private readonly bool _overwriteReachable;
        private readonly MprFeatureSet _realizationalOut = new MprFeatureSet();
        private readonly MprFeatureSet _compoundingOut = new MprFeatureSet();
        private long _mprWork;

        public LookupReplay(Language language)
        {
            var allOut = new MprFeatureSet();
            foreach (Stratum stratum in language.Strata)
            {
                var rules = new List<IMorphologicalRule>(stratum.MorphologicalRules);
                rules.AddRange(stratum.AffixTemplates.SelectMany(t => t.Slots).SelectMany(s => s.Rules));
                foreach (IMorphologicalRule rule in rules.Distinct())
                {
                    switch (rule)
                    {
                        case AffixProcessRule affixRule:
                            _checksMpr |= affixRule.RequiredStemName != null;
                            foreach (AffixProcessAllomorph allo in affixRule.Allomorphs)
                            {
                                _checksMpr |= allo.RequiredMprFeatures.Count > 0 || allo.ExcludedMprFeatures.Count > 0;
                                allOut.UnionWith(allo.OutMprFeatures);
                            }
                            break;
                        case RealizationalAffixProcessRule realRule:
                            foreach (AffixProcessAllomorph allo in realRule.Allomorphs)
                            {
                                _realizationalOut.UnionWith(allo.OutMprFeatures);
                                allOut.UnionWith(allo.OutMprFeatures);
                            }
                            break;
                        case CompoundingRule compRule:
                            _compoundingOut.UnionWith(compRule.OutputProdRestrictionsMprFeatures);
                            foreach (CompoundingSubrule subrule in compRule.Subrules)
                                _compoundingOut.UnionWith(subrule.OutMprFeatures);
                            allOut.UnionWith(_compoundingOut);
                            break;
                    }
                }
            }
            _overwriteReachable = allOut.Any(f => f.Group != null && f.Group.Output == MprFeatureGroupOutput.Overwrite);
        }

        /// <summary>The MPR pass's units of work: one per feature-set match, union or copy.</summary>
        public long MprWork => Interlocked.Read(ref _mprWork);

        public long Work => MprWork;

        public bool IsFeasible(Word alternative)
        {
            if (!_checksMpr)
                return true;
            if (!(alternative.RootAllomorph?.Morpheme is LexEntry root) || root.Family != null)
                return true;

            long work = 0;
            bool feasible = IsMprFeasible(alternative, root, ref work);
            if (work > 0)
                Interlocked.Add(ref _mprWork, work);
            return feasible;
        }

        /// <summary>
        /// A synthesis succeeds only if every trail entry applies, in order, with its recorded allomorph (or any
        /// allomorph when none is recorded), so one entry that cannot apply is enough to reject. The checks
        /// decided without shapes are the stem name and the MPR features.
        /// <para>
        /// The MPR set a rule sees is the root's, plus the outputs of earlier trail entries, plus outputs of
        /// realizational rules (which are not on the trail and may fire anywhere), minus features removed by
        /// overwrite groups. The set used for a required check is a superset of that set: it ignores removals
        /// and adds every output that might have been added. A required check that fails against a superset
        /// fails against the actual set, for both All and Any groups. The set used for an excluded check is a
        /// subset: the root's features plus outputs that certainly apply (those of recorded allomorphs). It is
        /// used only when no output can reach an overwrite group and no compounding rule is on the trail, so
        /// nothing is removed or added unseen. An excluded check that fails against a subset fails against the
        /// actual set, for both All and Any groups, because both tests are monotone in the set.
        /// </para>
        /// <para>
        /// Compounding rules and unknown trail entries only add their possible outputs, and they disable both the
        /// excluded check and the stem-name check, since the head may differ.
        /// </para>
        /// </summary>
        private bool IsMprFeasible(Word alternative, LexEntry root, ref long work)
        {
            StemName rootStemName = alternative.RootAllomorph.StemName;
            // The sets are cloned on first change, so a trail that adds no features copies nothing.
            MprFeatureSet lower = root.MprFeatures;
            MprFeatureSet extra = _realizationalOut;
            bool lowerOwned = false;
            bool extraOwned = false;
            bool excludedOk = !_overwriteReachable;
            bool stemNameOk = true;
            IReadOnlyList<IMorphologicalRule> rules = alternative.MorphologicalRuleApplications;
            IReadOnlyList<Allomorph> allomorphs = alternative.AllomorphApplications;
            for (int i = rules.Count - 1; i >= 0; i--)
            {
                if (!(rules[i] is AffixProcessRule rule))
                {
                    if (_compoundingOut.Count > 0)
                    {
                        work++;
                        Own(ref extra, ref extraOwned).UnionWith(_compoundingOut);
                    }
                    excludedOk = false;
                    stemNameOk = false;
                    continue;
                }
                if (stemNameOk && rule.RequiredStemName != null && rule.RequiredStemName != rootStemName)
                    return false;

                var recorded = allomorphs[i] as AffixProcessAllomorph;
                MprFeatureSet upper = null;
                bool anyPossible = false;
                if (recorded != null)
                {
                    anyPossible = IsAllomorphPossible(recorded, lower, extra, excludedOk, ref upper, ref work);
                }
                else
                {
                    foreach (AffixProcessAllomorph allo in rule.Allomorphs)
                    {
                        if (IsAllomorphPossible(allo, lower, extra, excludedOk, ref upper, ref work))
                        {
                            anyPossible = true;
                            break;
                        }
                    }
                }
                if (!anyPossible)
                    return false;

                if (recorded != null)
                {
                    if (recorded.OutMprFeatures.Count > 0)
                    {
                        work++;
                        Own(ref lower, ref lowerOwned).UnionWith(recorded.OutMprFeatures);
                    }
                }
                else
                {
                    foreach (AffixProcessAllomorph allo in rule.Allomorphs)
                    {
                        if (allo.OutMprFeatures.Count > 0)
                        {
                            work++;
                            Own(ref extra, ref extraOwned).UnionWith(allo.OutMprFeatures);
                        }
                    }
                }
            }
            return true;
        }

        private static bool IsAllomorphPossible(
            AffixProcessAllomorph allo,
            MprFeatureSet lower,
            MprFeatureSet extra,
            bool excludedOk,
            ref MprFeatureSet upper,
            ref long work
        )
        {
            if (allo.RequiredMprFeatures.Count > 0)
            {
                if (upper == null)
                {
                    work++;
                    upper = lower.Clone();
                    upper.UnionWith(extra);
                }
                work++;
                if (!allo.RequiredMprFeatures.IsMatchRequired(upper, out _))
                    return false;
            }
            if (!excludedOk || allo.ExcludedMprFeatures.Count == 0)
                return true;
            work++;
            return allo.ExcludedMprFeatures.IsMatchExcluded(lower, out _);
        }

        private static MprFeatureSet Own(ref MprFeatureSet set, ref bool owned)
        {
            if (!owned)
            {
                set = set.Clone();
                owned = true;
            }
            return set;
        }
    }
}
