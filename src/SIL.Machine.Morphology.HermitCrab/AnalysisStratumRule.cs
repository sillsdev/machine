using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal class AnalysisStratumRule : IRule<Word, ShapeNode>
    {
        private readonly RuleCascade<Word, ShapeNode> _mrulesRule;
        private readonly IRule<Word, ShapeNode> _prulesRule;
        private readonly RuleBatch<Word, ShapeNode> _templatesRule;
        private readonly Stratum _stratum;
        private readonly Morpher _morpher;

        public AnalysisStratumRule(Morpher morpher, Stratum stratum)
        {
            _stratum = stratum;
            _morpher = morpher;
            _prulesRule = new LinearRuleCascade<Word, ShapeNode>(
                stratum.PhonologicalRules.Select(prule => CompilePhonologicalRule(prule, morpher)).Reverse()
            );
            _templatesRule = new RuleBatch<Word, ShapeNode>(
                stratum.AffixTemplates.Select(template => CompileAffixTemplate(template, morpher)),
                false,
                FreezableEqualityComparer<Word>.Default
            );
            _mrulesRule = CreateMorphologicalRules(
                stratum.MorphologicalRules.Select(mrule => CompileMorphologicalRule(mrule, morpher)).Reverse()
            );
        }

        private RuleCascade<Word, ShapeNode> CreateMorphologicalRules(IEnumerable<IRule<Word, ShapeNode>> rules)
        {
            switch (_stratum.MorphologicalRuleOrder)
            {
                case MorphologicalRuleOrder.Linear:
                    // Use PermutationRuleCascade instead of LinearRuleCascade
                    // because morphological rules should be considered optional
                    // during unapplication (they are obligatory during application,
                    // but we don't know they have been applied during unapplication).
                    return new PermutationRuleCascade<Word, ShapeNode>(
                        rules,
                        true,
                        FreezableEqualityComparer<Word>.Default
                    );
                case MorphologicalRuleOrder.Unordered:
                    if (_morpher.MaxDegreeOfParallelism == 1)
                    {
                        return new CombinationRuleCascade<Word, ShapeNode>(
                            rules,
                            true,
                            FreezableEqualityComparer<Word>.Default
                        );
                    }
                    else
                    {
                        return new ParallelCombinationRuleCascade<Word, ShapeNode>(
                            rules,
                            true,
                            FreezableEqualityComparer<Word>.Default
                        )
                        {
                            MaxDegreeOfParallelism = _morpher.MaxDegreeOfParallelism,
                        };
                    }
            }
            return null;
        }

        private IRule<Word, ShapeNode> CompileAffixTemplate(AffixTemplate template, Morpher morpher)
        {
            try
            {
                return template.CompileAnalysisRule(morpher);
            }
            catch (Exception e)
            {
                throw new CompileException("Could not compile affix template named " + template.Name, e);
            }
        }

        private IRule<Word, ShapeNode> CompileMorphologicalRule(IMorphologicalRule mrule, Morpher morpher)
        {
            try
            {
                return mrule.CompileAnalysisRule(morpher);
            }
            catch (Exception e)
            {
                throw new CompileException("Could not compile morphological rule named " + mrule.Name, e);
            }
        }

        private IRule<Word, ShapeNode> CompilePhonologicalRule(IPhonologicalRule prule, Morpher morpher)
        {
            try
            {
                return prule.CompileAnalysisRule(morpher);
            }
            catch (Exception e)
            {
                throw new CompileException("Could not compile phonological rule named " + prule.Name, e);
            }
        }

        public IEnumerable<Word> Apply(Word input)
        {
            // Standalone stratum application is uncapped.
            int alternativeCount = 0;
            return Apply(input, ref alternativeCount, 0);
        }

        internal IEnumerable<Word> Apply(Word input, ref int alternativeCount, int maxAlternatives)
        {
            RuleCascade<Word, ShapeNode> morphologicalRules = _mrulesRule;
            RuleBatch<Word, ShapeNode> templates = _templatesRule;
            if (maxAlternatives > 0)
            {
                // Limits belong to this invocation; compiled child rules remain shared and read-only.
                morphologicalRules = CreateMorphologicalRules(_mrulesRule.Rules);
                morphologicalRules.MaxAlternatives = maxAlternatives;
                templates = new RuleBatch<Word, ShapeNode>(_templatesRule.Rules, false, _templatesRule.Comparer)
                {
                    MaxAlternatives = maxAlternatives,
                };
            }

            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.BeginUnapplyStratum(_stratum, input);

            Word origInput = input;
            input = input.Clone();
            input.Stratum = _stratum;
            if (_morpher.IsBlockingStratum(_stratum))
                input.FinalTemplateState = FinalTemplateState.None;

            _prulesRule.Apply(input);
            input.Freeze();
            IDictionary<AnalysisMergeKey, Word> wordCache = null;
            // Don't merge if tracing because it messes up the tracing.
            bool mergeEquivalentAnalyses = _morpher.MergeEquivalentAnalyses && !_morpher.TraceManager.IsTracing;
            if (mergeEquivalentAnalyses)
                wordCache = new Dictionary<AnalysisMergeKey, Word>();

            // AnalysisStratumRule.Apply should cover the inverse of SynthesisStratumRule.Apply.
            IEnumerable<Word> mruleOutWords = ApplyTemplates(input, morphologicalRules, templates)
                .Concat(ApplyMorphologicalRules(input, morphologicalRules, templates));
            Debug.Assert(mruleOutWords != null);

            var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default) { input };
            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.EndUnapplyStratum(_stratum, input);
            foreach (Word mruleOutWord in mruleOutWords)
            {
                alternativeCount++;
                if (maxAlternatives > 0 && alternativeCount > maxAlternatives)
                {
                    // Stops before full enumeration because ApplyTemplates and ApplyMorphologicalRules use yield return.
                    throw new MaxAlternativesExceededException("MaxAlternatives exceeded");
                }
                // Skip intermediate sources from phonological rules, templates, and morphological rules.
                mruleOutWord.Source = origInput;
                AnalysisMergeKey key = default;
                if (mergeEquivalentAnalyses)
                {
                    key = new AnalysisMergeKey(mruleOutWord);
                    if (wordCache.TryGetValue(key, out Word canonicalWord))
                    {
                        canonicalWord.Alternatives.Add(mruleOutWord);
                        continue;
                    }
                }
                Word newMruleOutWord = mruleOutWord;
                // Only cache a canonical that made it into the output. Two words can have different keys yet
                // be Word.ValueEquals (UnappliedRuleCounts also counts realizational rules, which never enter
                // _mruleApps), and a rejected canonical would swallow every later word with its key.
                if (output.Add(newMruleOutWord) && mergeEquivalentAnalyses)
                    wordCache[key] = newMruleOutWord;
                if (_morpher.TraceManager.IsTracing)
                    _morpher.TraceManager.EndUnapplyStratum(_stratum, newMruleOutWord);
            }
            return output;
        }

        private IEnumerable<Word> ApplyMorphologicalRules(
            Word input,
            RuleCascade<Word, ShapeNode> morphologicalRules,
            RuleBatch<Word, ShapeNode> templates
        )
        {
            foreach (
                Word mruleOutWord in morphologicalRules.Apply(input).Distinct(FreezableEqualityComparer<Word>.Default)
            )
            {
                switch (_stratum.MorphologicalRuleOrder)
                {
                    case MorphologicalRuleOrder.Linear:
                        yield return mruleOutWord;
                        break;

                    case MorphologicalRuleOrder.Unordered:
                        foreach (Word tempOutWord in ApplyTemplates(mruleOutWord, morphologicalRules, templates))
                            yield return tempOutWord;
                        yield return mruleOutWord;
                        break;
                }
            }
        }

        private IEnumerable<Word> ApplyTemplates(
            Word input,
            RuleCascade<Word, ShapeNode> morphologicalRules,
            RuleBatch<Word, ShapeNode> templates
        )
        {
            foreach (Word tempOutWord in templates.Apply(input).Distinct(FreezableEqualityComparer<Word>.Default))
            {
                switch (_stratum.MorphologicalRuleOrder)
                {
                    case MorphologicalRuleOrder.Linear:
                        foreach (Word outWord in ApplyMorphologicalRules(tempOutWord, morphologicalRules, templates))
                            yield return outWord;
                        if (!FreezableEqualityComparer<Word>.Default.Equals(input, tempOutWord))
                            yield return tempOutWord;
                        break;

                    case MorphologicalRuleOrder.Unordered:
                        if (!FreezableEqualityComparer<Word>.Default.Equals(input, tempOutWord))
                        {
                            foreach (
                                Word outWord in ApplyMorphologicalRules(tempOutWord, morphologicalRules, templates)
                            )
                                yield return outWord;
                            yield return tempOutWord;
                        }
                        break;
                }
            }
        }

        internal readonly struct AnalysisMergeKey : IEquatable<AnalysisMergeKey>
        {
            private readonly Shape _shape;
            private readonly Stratum _stratum;
            private readonly FeatureStruct _syntacticFS;
            private readonly FeatureStruct _realizationalFS;
            private readonly int _nonHeadCount;
            private readonly IReadOnlyDictionary<IMorphologicalRule, int> _ruleCounts;
            private readonly FinalTemplateState _finalTemplateState;
            private readonly int _hashCode;

            // Freezes word.SyntacticFeatureStruct, which Word.Freeze leaves unfrozen.
            public AnalysisMergeKey(Word word)
            {
                if (!word.IsFrozen)
                    throw new ArgumentException(
                        "The word must be frozen before equivalent analyses can be merged.",
                        nameof(word)
                    );

                _shape = word.Shape;
                _stratum = word.Stratum;
                _syntacticFS = word.SyntacticFeatureStruct;
                _realizationalFS = word.RealizationalFeatureStruct;
                _nonHeadCount = word.NonHeadCount;
                _ruleCounts = word.UnappliedRuleCounts;
                _finalTemplateState = word.FinalTemplateState;

                _shape.Freeze();
                _syntacticFS.Freeze();
                _realizationalFS.Freeze();

                int hash = 17;
                hash = hash * 31 + _shape.GetFrozenHashCode();
                hash = hash * 31 + (_stratum?.GetHashCode() ?? 0);
                hash = hash * 31 + _syntacticFS.GetFrozenHashCode();
                hash = hash * 31 + _realizationalFS.GetFrozenHashCode();
                hash = hash * 31 + _nonHeadCount;
                hash = hash * 31 + _finalTemplateState.GetHashCode();
                if (_ruleCounts != null)
                {
                    int multisetHash = 0;
                    foreach (KeyValuePair<IMorphologicalRule, int> kvp in _ruleCounts)
                        multisetHash ^= (kvp.Key.GetHashCode() * 397) ^ kvp.Value;
                    hash = hash * 31 + multisetHash;
                }
                _hashCode = hash;
            }

            public override int GetHashCode() => _hashCode;

            public override bool Equals(object obj) => obj is AnalysisMergeKey other && Equals(other);

            public bool Equals(AnalysisMergeKey other)
            {
                if (_hashCode != other._hashCode)
                    return false;
                if (_nonHeadCount != other._nonHeadCount || !ReferenceEquals(_stratum, other._stratum))
                    return false;
                if (!_shape.ValueEquals(other._shape))
                    return false;
                if (
                    !_syntacticFS.ValueEquals(other._syntacticFS)
                    || !_realizationalFS.ValueEquals(other._realizationalFS)
                )
                    return false;
                if (_finalTemplateState != other._finalTemplateState)
                    return false;
                return RuleCountsEqual(_ruleCounts, other._ruleCounts);
            }

            private static bool RuleCountsEqual(
                IReadOnlyDictionary<IMorphologicalRule, int> a,
                IReadOnlyDictionary<IMorphologicalRule, int> b
            )
            {
                int aCount = a?.Count ?? 0;
                int bCount = b?.Count ?? 0;
                if (aCount != bCount)
                    return false;
                if (aCount == 0)
                    return true;
                foreach (KeyValuePair<IMorphologicalRule, int> kvp in a)
                {
                    if (!b.TryGetValue(kvp.Key, out int otherCount) || otherCount != kvp.Value)
                        return false;
                }
                return true;
            }
        }
    }
}
