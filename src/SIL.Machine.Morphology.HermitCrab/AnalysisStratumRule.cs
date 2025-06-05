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
        private readonly IRule<Word, ShapeNode> _mrulesRule;
        private readonly IRule<Word, ShapeNode> _prulesRule;
        private readonly IRule<Word, ShapeNode> _templatesRule;
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
#if SINGLE_THREADED
                    _mrulesRule = new CombinationRuleCascade<Word, ShapeNode>(
                        mrules,
                        true,
                        FreezableEqualityComparer<Word>.Default
                    );
#else
                    _mrulesRule = new ParallelCombinationRuleCascade<Word, ShapeNode>(
                        mrules,
                        true,
                        FreezableEqualityComparer<Word>.Default
                    );
#endif
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

        public IEnumerable<Word> Apply(Word input)
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

        private IEnumerable<Word> ApplyTemplates(Word input)
        {
            foreach (Word tempOutWord in _templatesRule.Apply(input).Distinct(FreezableEqualityComparer<Word>.Default))
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
