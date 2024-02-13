using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal class SynthesisStratumRule : IRule<Word, ShapeNode>
    {
        private readonly IRule<Word, ShapeNode> _mrulesRule;
        private readonly IRule<Word, ShapeNode> _prulesRule;
        private readonly SynthesisAffixTemplatesRule _templatesRule;
        private readonly Stratum _stratum;
        private readonly Morpher _morpher;

        public SynthesisStratumRule(Morpher morpher, Stratum stratum)
        {
            _templatesRule = new SynthesisAffixTemplatesRule(morpher, stratum);
            _mrulesRule = null;
            IEnumerable<IRule<Word, ShapeNode>> mrules = stratum.MorphologicalRules.Select(mrule =>
                mrule.CompileSynthesisRule(morpher)
            );
            switch (stratum.MorphologicalRuleOrder)
            {
                case MorphologicalRuleOrder.Linear:
                    _mrulesRule = new LinearRuleCascade<Word, ShapeNode>(
                        mrules,
                        true,
                        FreezableEqualityComparer<Word>.Default
                    );
                    break;
                case MorphologicalRuleOrder.Unordered:
                    _mrulesRule = new CombinationRuleCascade<Word, ShapeNode>(
                        mrules,
                        true,
                        FreezableEqualityComparer<Word>.Default
                    );
                    break;
            }
            _prulesRule = new LinearRuleCascade<Word, ShapeNode>(
                stratum.PhonologicalRules.Select(prule => prule.CompileSynthesisRule(morpher))
            );
            _stratum = stratum;
            _morpher = morpher;
        }

        public IEnumerable<Word> Apply(Word input)
        {
            if (!_morpher.RuleSelector(_stratum) || input.RootAllomorph.Morpheme.Stratum.Depth > _stratum.Depth)
                return input.ToEnumerable();

            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.BeginApplyStratum(_stratum, input);

            var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
            foreach (Word mruleOutWord in ApplyMorphologicalRules(input).Concat(ApplyTemplates(input)))
            {
                if (!(mruleOutWord.IsLastAppliedRuleFinal ?? false))
                {
                    if (_morpher.TraceManager.IsTracing)
                        _morpher.TraceManager.NonFinalTemplateAppliedLast(_stratum, mruleOutWord);
                }
                else if (mruleOutWord.HasRemainingRulesFromStratum(_stratum))
                {
                    if (_morpher.TraceManager.IsTracing)
                        _morpher.TraceManager.Failed(
                            _morpher.Language,
                            mruleOutWord,
                            FailureReason.PartialParse,
                            null,
                            null
                        );
                }
                else
                {
                    Word newWord = mruleOutWord.Clone();
                    _prulesRule.Apply(newWord);
                    newWord.IsLastAppliedRuleFinal = null;
                    newWord.Freeze();
                    if (_morpher.TraceManager.IsTracing)
                        _morpher.TraceManager.EndApplyStratum(_stratum, newWord);
                    output.Add(newWord);
                }
            }
            if (_morpher.TraceManager.IsTracing && output.Count == 0)
                _morpher.TraceManager.EndApplyStratum(_stratum, input);
            return output;
        }

        private IEnumerable<Word> ApplyMorphologicalRules(Word input)
        {
            foreach (Word mruleOutWord in _mrulesRule.Apply(input))
            {
                if (mruleOutWord.IsLastAppliedRuleFinal ?? false)
                    yield return mruleOutWord;
                else
                    foreach (Word tempOutWord in ApplyTemplates(mruleOutWord))
                        yield return tempOutWord;
            }
        }

        private IEnumerable<Word> ApplyTemplates(Word input)
        {
            foreach (Word tempOutWord in _templatesRule.Apply(input))
            {
                switch (_stratum.MorphologicalRuleOrder)
                {
                    case MorphologicalRuleOrder.Linear:
                        yield return tempOutWord;
                        break;

                    case MorphologicalRuleOrder.Unordered:
                        if (!FreezableEqualityComparer<Word>.Default.Equals(input, tempOutWord))
                            foreach (Word outWord in ApplyMorphologicalRules(tempOutWord))
                                yield return outWord;
                        yield return tempOutWord;
                        break;
                }
            }
        }
    }
}
