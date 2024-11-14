using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal class AnalysisLanguageRule : IRule<Word, ShapeNode>
    {
        private readonly Morpher _morpher;
        private readonly List<Stratum> _strata;
        private readonly List<IRule<Word, ShapeNode>> _rules;

        public AnalysisLanguageRule(Morpher morpher, Language language)
        {
            _morpher = morpher;
            _strata = language.Strata.Reverse().ToList();
            _rules = _strata.Select(stratum => stratum.CompileAnalysisRule(morpher)).ToList();
        }

        public IEnumerable<Word> Apply(Word input)
        {
            var inputSet = new HashSet<Word>(FreezableEqualityComparer<Word>.Default) { input };
            var tempSet = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
            var results = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
            for (int i = 0; i < _rules.Count && inputSet.Count > 0; i++)
            {
                if (!_morpher.RuleSelector(_strata[i]))
                    continue;

                HashSet<Word> outputSet = tempSet;
                outputSet.Clear();
                AnalysisStratumRule stratumRule = _rules[i] as AnalysisStratumRule;
                stratumRule?.InitializeDuplicationDetection();

                foreach (Word inData in inputSet)
                {
                    foreach (Word outData in _rules[i].Apply(inData))
                    {
                        outputSet.Add(outData);
                        results.Add(outData);
                    }
                }

                stratumRule?.ClearDuplicationDetection();
                tempSet = inputSet;
                inputSet = outputSet;
            } 

            return results;
        }
    }
}
