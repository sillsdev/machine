using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    /// <summary>
    /// This class represents a metathesis rule. Metathesis rules are phonological rules that
    /// reorder segments.
    /// </summary>
    public class MetathesisRule : HCRuleBase, IPhonologicalRule
    {
        public MetathesisRule()
        {
            Pattern = Pattern<Word, int>.New().Value;
        }

        public Direction Direction { get; set; }

        public Pattern<Word, int> Pattern { get; set; }

        public string LeftSwitchName { get; set; }

        public string RightSwitchName { get; set; }

        public override IRule<Word, int> CompileAnalysisRule(Morpher morpher)
        {
            return new AnalysisMetathesisRule(morpher, this);
        }

        public override IRule<Word, int> CompileSynthesisRule(Morpher morpher)
        {
            return new SynthesisMetathesisRule(morpher, this);
        }
    }
}
