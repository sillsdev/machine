using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        private int _maxAlternatives;

        public AnalysisStratumRule(Morpher morpher, Stratum stratum)
        {
            _stratum = stratum;
            _morpher = morpher;
            _prulesRule = new LinearRuleCascade<Word, ShapeNode>(
                stratum.PhonologicalRules.Select(prule => CompilePhonologicalRule(prule, morpher)).Reverse()
            );
            IEnumerable<IRule<Word, ShapeNode>> templates = stratum.AffixTemplates.Select(template =>
                CompileAffixTemplate(template, morpher)
            );
            _templatesRule = new RuleBatch<Word, ShapeNode>(templates, false, FreezableEqualityComparer<Word>.Default);
            _mrulesRule = null;
            IEnumerable<IRule<Word, ShapeNode>> mrules = stratum
                .MorphologicalRules.Select(mrule => CompileMorphologicalRule(mrule, morpher))
                .Reverse();
            switch (stratum.MorphologicalRuleOrder)
            {
                case MorphologicalRuleOrder.Linear:
                    // Use PermutationRuleCascade instead of LinearRuleCascade
                    // because morphological rules should be considered optional
                    // during unapplication (they are obligatory during application,
                    // but we don't know they have been applied during unapplication).
                    _mrulesRule = new PermutationRuleCascade<Word, ShapeNode>(
                        mrules,
                        true,
                        FreezableEqualityComparer<Word>.Default
                    );
                    break;
                case MorphologicalRuleOrder.Unordered:
                    if (morpher.MaxDegreeOfParallelism == 1)
                    {
                        _mrulesRule = new CombinationRuleCascade<Word, ShapeNode>(
                            mrules,
                            true,
                            FreezableEqualityComparer<Word>.Default
                        );
                    }
                    else
                    {
                        _mrulesRule = new ParallelCombinationRuleCascade<Word, ShapeNode>(
                            mrules,
                            true,
                            FreezableEqualityComparer<Word>.Default
                        )
                        {
                            MaxDegreeOfParallelism = morpher.MaxDegreeOfParallelism,
                        };
                    }
                    break;
            }
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

        public int MaxAlternatives
        {
            get { return _maxAlternatives; }
            set
            {
                _maxAlternatives = value;
                _mrulesRule.MaxAlternatives = value;
                _templatesRule.MaxAlternatives = value;
            }
        }

        public IEnumerable<Word> Apply(Word input)
        {
            int alternativeCount = 0;
            return Apply(input, ref alternativeCount);
        }

        internal IEnumerable<Word> Apply(Word input, ref int alternativeCount)
        {
            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.BeginUnapplyStratum(_stratum, input);

            Word origInput = input;
            input = input.Clone();
            input.Stratum = _stratum;
            if (_morpher.IsBlockingStratum(_stratum))
                input.FinalTemplateState = FinalTemplateState.None;
            _prulesRule.Apply(input);
            input.Freeze();

            ConcurrentDictionary<Word, Word> wordCache = null;
            ConcurrentDictionary<AnalysisMergeKey, Word> keyCache = null;
            // Don't merge if tracing because it messes up the tracing.
            bool mergeEquivalentAnalyses = _morpher.MergeEquivalentAnalyses && !_morpher.TraceManager.IsTracing;
            if (mergeEquivalentAnalyses)
                keyCache = new ConcurrentDictionary<AnalysisMergeKey, Word>();
            else
                wordCache = new ConcurrentDictionary<Word, Word>(FreezableEqualityComparer<Word>.Default);

            // Use a local variable so FilterResults can access it.
            int localAlternativeCount = alternativeCount;

            IEnumerable<Word> FilterResults(IEnumerable<Word> results)
            {
                foreach (Word result in results)
                {
                    if (mergeEquivalentAnalyses)
                    {
                        var key = new AnalysisMergeKey(result);
                        Word canonicalWord = keyCache.GetOrAdd(key, result);
                        if (!canonicalWord.ValueEquals(result))
                        {
                            canonicalWord.Alternatives.Add(result);
                            continue;
                        }
                    }
                    else
                    {
                        // Skip intermediate sources from phonological rules, templates, and morphological rules.
                        result.Source = origInput;
                        Word canonicalWord = wordCache.GetOrAdd(result, result);
                        if (canonicalWord != result)
                            continue;
                    }

                    Interlocked.Increment(ref localAlternativeCount);
                    if (_maxAlternatives > 0 && localAlternativeCount > _maxAlternatives)
                        throw new MaxAlternativesExceededException("MaxAlternatives exceeded");

                    yield return result;
                }
            }

            // AnalysisStratumRule.Apply should cover the inverse of SynthesisStratumRule.Apply.
            IList<Word> mruleOutWords = ApplyTemplates(input, FilterResults)
                .Concat(ApplyMorphologicalRules(input, FilterResults))
                .ToList();
            Debug.Assert(mruleOutWords != null);
            mruleOutWords.Add(input);

            if (_morpher.TraceManager.IsTracing)
            {
                foreach (Word outWord in mruleOutWords)
                    _morpher.TraceManager.EndUnapplyStratum(_stratum, outWord);
            }

            alternativeCount = localAlternativeCount;
            return mruleOutWords;
        }

        private IEnumerable<Word> ApplyMorphologicalRules(
            Word input,
            Func<IEnumerable<Word>, IEnumerable<Word>> filterResults
        )
        {
            foreach (
                Word mruleOutWord in _mrulesRule
                    .Apply(input, filterResults)
                    .Distinct(FreezableEqualityComparer<Word>.Default)
            )
            {
                switch (_stratum.MorphologicalRuleOrder)
                {
                    case MorphologicalRuleOrder.Linear:
                        yield return mruleOutWord;
                        break;

                    case MorphologicalRuleOrder.Unordered:
                        foreach (Word tempOutWord in ApplyTemplates(mruleOutWord, filterResults))
                            yield return tempOutWord;
                        yield return mruleOutWord;
                        break;
                }
            }
        }

        private IEnumerable<Word> ApplyTemplates(Word input, Func<IEnumerable<Word>, IEnumerable<Word>> filterResults)
        {
            foreach (
                Word tempOutWord in _templatesRule
                    .Apply(input, filterResults)
                    .Distinct(FreezableEqualityComparer<Word>.Default)
            )
            {
                switch (_stratum.MorphologicalRuleOrder)
                {
                    case MorphologicalRuleOrder.Linear:
                        foreach (Word outWord in ApplyMorphologicalRules(tempOutWord, filterResults))
                            yield return outWord;
                        if (!FreezableEqualityComparer<Word>.Default.Equals(input, tempOutWord))
                            yield return tempOutWord;
                        break;

                    case MorphologicalRuleOrder.Unordered:
                        if (!FreezableEqualityComparer<Word>.Default.Equals(input, tempOutWord))
                        {
                            foreach (Word outWord in ApplyMorphologicalRules(tempOutWord, filterResults))
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
