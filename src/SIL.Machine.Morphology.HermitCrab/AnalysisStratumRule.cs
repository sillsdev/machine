using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Machine.Annotations;
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
            _templatesRule = new RuleBatch<Word, ShapeNode>(
                stratum.AffixTemplates.Select(template => CompileAffixTemplate(template, morpher)),
                false,
                FreezableEqualityComparer<Word>.Default
            );
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
                    // Sequential (and memoized, see MemoizedCombinationRuleCascade) when the caller caps
                    // within-word parallelism; parallel cascade otherwise.
                    _mrulesRule =
                        morpher.MaxDegreeOfParallelism == 1
                            ? (IRule<Word, ShapeNode>)
                                new MemoizedCombinationRuleCascade(mrules, FreezableEqualityComparer<Word>.Default)
                            : new ParallelCombinationRuleCascade<Word, ShapeNode>(
                                mrules,
                                true,
                                FreezableEqualityComparer<Word>.Default
                            );
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

            _prulesRule.Apply(input);
            input.Freeze();
            IDictionary<Shape, Word> shapeWord = null;
            // Don't merge if tracing because it messes up the tracing.
            bool mergeEquivalentAnalyses = _morpher.MergeEquivalentAnalyses && !_morpher.TraceManager.IsTracing;
            if (mergeEquivalentAnalyses)
                shapeWord = new Dictionary<Shape, Word>(FreezableEqualityComparer<Shape>.Default);

            // AnalysisStratumRule.Apply should cover the inverse of SynthesisStratumRule.Apply.
            IEnumerable<Word> mruleOutWords = ApplyTemplates(input).Concat(ApplyMorphologicalRules(input));
            Debug.Assert(mruleOutWords != null);

            var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default) { input };
            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.EndUnapplyStratum(_stratum, input);
            foreach (Word mruleOutWord in mruleOutWords)
            {
                alternativeCount++;
                if (_maxAlternatives > 0 && alternativeCount > _maxAlternatives)
                {
                    // Stops before full enumeration because ApplyTemplates and ApplyMorphologicalRules use yield return.
                    throw new MaxAlternativesExceededException("MaxAlternatives exceeded");
                }
                // Skip intermediate sources from phonological rules, templates, and morphological rules.
                mruleOutWord.Source = origInput;
                if (mergeEquivalentAnalyses)
                {
                    Shape shape = mruleOutWord.Shape;
                    Word canonicalWord;
                    if (shapeWord.TryGetValue(shape, out canonicalWord))
                    {
                        canonicalWord.Alternatives.Add(mruleOutWord);
                        continue;
                    }
                    shapeWord[shape] = mruleOutWord;
                }
                output.Add(mruleOutWord);
                if (_morpher.TraceManager.IsTracing)
                    _morpher.TraceManager.EndUnapplyStratum(_stratum, mruleOutWord);
            }
            return output;
        }

        private IEnumerable<Word> ApplyMorphologicalRules(Word input)
        {
            foreach (Word mruleOutWord in _mrulesRule.Apply(input).Distinct(FreezableEqualityComparer<Word>.Default))
            {
                switch (_stratum.MorphologicalRuleOrder)
                {
                    case MorphologicalRuleOrder.Linear:
                        yield return mruleOutWord;
                        break;

                    case MorphologicalRuleOrder.Unordered:
                        foreach (Word tempOutWord in ApplyTemplates(mruleOutWord))
                            yield return tempOutWord;
                        yield return mruleOutWord;
                        break;
                }
            }
        }

        // Test/reporting hooks (memoization.md's standing hit/miss-count requirement), mirroring
        // MemoizedCombinationRuleCascade's DiagMemoHits/DiagNogoodHits split.
        internal static long DiagTemplateMemoHits;
        internal static long DiagTemplateNogoodHits;

        // Runs the affix-template battery for `input`, memoized by AnalysisStateKey (memoization.md),
        // same replay mechanism as MemoizedCombinationRuleCascade -- see AnalysisScope.InProgress for
        // why no re-entry guard is needed here. Measured motivation (an archive prototype benchmark on
        // a Bantu-template grammar): this battery accounted for the large majority of parse wall time
        // once the mrule cascade's own memo had already shrunk its own share to near-nothing.
        private IEnumerable<Word> ApplyTemplateBattery(Word input)
        {
            AnalysisScope scope = input.AnalysisScope;
            if (scope == null || _morpher.MaxDegreeOfParallelism != 1)
                return _templatesRule.Apply(input);

            var key = new AnalysisStateKey(input);
            if (scope.TemplateMemo.TryGetValue(key, out MemoEntry entry))
            {
                if (entry.Results.Count == 0)
                {
                    DiagTemplateNogoodHits++;
                    return Enumerable.Empty<Word>();
                }
                var replayed = new List<Word>(entry.Results.Count);
                foreach (Word stored in entry.Results)
                    replayed.Add(stored.ReplayOnto(input, entry.MruleTrailPrefixLength, entry.NonHeadPrefixLength));
                DiagTemplateMemoHits++;
                return replayed;
            }

            var results = new List<Word>(_templatesRule.Apply(input));
            if (scope.HasTemplateMemoCapacity)
            {
                scope.TemplateMemo.TryAdd(
                    key,
                    new MemoEntry(results, input.MorphologicalRuleTrailLength, input.NonHeadCount)
                );
            }
            return results;
        }

        private IEnumerable<Word> ApplyTemplates(Word input)
        {
            foreach (Word tempOutWord in ApplyTemplateBattery(input).Distinct(FreezableEqualityComparer<Word>.Default))
            {
                switch (_stratum.MorphologicalRuleOrder)
                {
                    case MorphologicalRuleOrder.Linear:
                        foreach (Word outWord in ApplyMorphologicalRules(tempOutWord))
                            yield return outWord;
                        if (!FreezableEqualityComparer<Word>.Default.Equals(input, tempOutWord))
                            yield return tempOutWord;
                        break;

                    case MorphologicalRuleOrder.Unordered:
                        if (!FreezableEqualityComparer<Word>.Default.Equals(input, tempOutWord))
                        {
                            foreach (Word outWord in ApplyMorphologicalRules(tempOutWord))
                                yield return outWord;
                            yield return tempOutWord;
                        }
                        break;
                }
            }
        }
    }
}
