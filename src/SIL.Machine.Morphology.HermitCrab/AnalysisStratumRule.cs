using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal class AnalysisStratumRule : IRule<Word, int>
    {
        private readonly IRule<Word, int> _mrulesRule;
        private readonly PermutationRuleCascade<Word, int> _permutationCascade;
        private readonly IRule<Word, int> _prulesRule;
        private readonly IRule<Word, int> _templatesRule;
        private readonly Stratum _stratum;
        private readonly Morpher _morpher;

        public AnalysisStratumRule(Morpher morpher, Stratum stratum)
        {
            _stratum = stratum;
            _morpher = morpher;
            _prulesRule = new LinearRuleCascade<Word, int>(
                stratum.PhonologicalRules.Select(prule => CompilePhonologicalRule(prule, morpher)).Reverse()
            );
            _templatesRule = new RuleBatch<Word, int>(
                stratum.AffixTemplates.Select(template => CompileAffixTemplate(template, morpher)),
                false,
                FreezableEqualityComparer<Word>.Default
            );
            _mrulesRule = null;
            IEnumerable<IRule<Word, int>> mrules = stratum
                .MorphologicalRules.Select(mrule => CompileMorphologicalRule(mrule, morpher))
                .Reverse();
            switch (stratum.MorphologicalRuleOrder)
            {
                case MorphologicalRuleOrder.Linear:
                    // Use PermutationRuleCascade instead of LinearRuleCascade
                    // because morphological rules should be considered optional
                    // during unapplication (they are obligatory during application,
                    // but we don't know they have been applied during unapplication).
                    _permutationCascade = new PermutationRuleCascade<Word, int>(
                        mrules,
                        true,
                        FreezableEqualityComparer<Word>.Default
                    );
                    _mrulesRule = _permutationCascade;
                    break;
                case MorphologicalRuleOrder.Unordered:
                    // Single-threaded when the caller caps within-word parallelism (e.g. it
                    // parallelizes across words itself); parallel cascade otherwise.
                    _mrulesRule =
                        morpher.MaxDegreeOfParallelism == 1
                            ? (IRule<Word, int>)
                                new CombinationRuleCascade<Word, int>(
                                    mrules,
                                    true,
                                    FreezableEqualityComparer<Word>.Default
                                )
                            : new ParallelCombinationRuleCascade<Word, int>(
                                mrules,
                                true,
                                FreezableEqualityComparer<Word>.Default
                            )
                            {
                                // Honor the within-word parallelism cap rather than running at
                                // the default (effectively unbounded) scheduler degree.
                                MaxDegreeOfParallelism = morpher.MaxDegreeOfParallelism,
                            };
                    break;
            }
        }

        private IRule<Word, int> CompileAffixTemplate(AffixTemplate template, Morpher morpher)
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

        private IRule<Word, int> CompileMorphologicalRule(IMorphologicalRule mrule, Morpher morpher)
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

        private IRule<Word, int> CompilePhonologicalRule(IPhonologicalRule prule, Morpher morpher)
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

        private bool ExceedsShapeGrowth(Word word)
        {
            return _morpher.MaxAnalysisShapeGrowth >= 0
                && word.ParseContext != null
                && word.Shape.Count > word.ParseContext.SurfaceLength + _morpher.MaxAnalysisShapeGrowth;
        }

        public IEnumerable<Word> Apply(Word input)
        {
            // Re-synced on every call rather than baked in at compile time: MaxRuleApplicationsPerWord
            // is a mutable Morpher property that callers set via object-initializer syntax after
            // construction (the same pattern MaxParseSteps/ParseTimeout use), which runs after this
            // rule was already compiled. No new knob per complexity-cap.md §5.3 — derived from the
            // existing per-word unapplication cap (0/unlimited maps to no depth limit).
            if (_permutationCascade != null)
                _permutationCascade.MaxDepth =
                    _morpher.MaxRuleApplicationsPerWord > 0 ? _morpher.MaxRuleApplicationsPerWord : -1;

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
                // Once the budget is gone, stop collecting outputs immediately rather than draining the
                // rest of an already-in-flight (but now-empty-yielding) rule cascade.
                if (input.ParseContext?.Exhausted == true)
                    break;

                // Prune candidates whose hypothesized underlying shape has grown too far past the
                // surface form — the truly unbounded generator (undone deletions, empty exponents).
                // Pruned here so they never reach lexical lookup or the next stratum.
                if (ExceedsShapeGrowth(mruleOutWord))
                {
                    continue;
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
                if (_morpher.MaxUnapplications > 0 && output.Count >= _morpher.MaxUnapplications)
                    break;
            }
            return output;
        }

        private IEnumerable<Word> ApplyMorphologicalRules(Word input)
        {
            foreach (Word mruleOutWord in _mrulesRule.Apply(input))
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

        private IEnumerable<Word> ApplyTemplates(Word input)
        {
            foreach (Word tempOutWord in _templatesRule.Apply(input))
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
