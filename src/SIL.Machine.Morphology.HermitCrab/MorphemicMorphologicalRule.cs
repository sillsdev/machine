using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    public abstract class MorphemicMorphologicalRule : Morpheme, IMorphologicalRule
    {
        public bool? IsCliticRuleUser;
        public string Name { get; set; }
        public bool IsTemplateRule { get; set; }
        internal bool IsCliticRule
        {
            get
            {
                if (IsCliticRuleUser == null)
                    return Stratum != null && Stratum.Name == "Clitics";
                return (bool)IsCliticRuleUser;
            }
        }

        public override MorphemeType MorphemeType
        {
            get { return MorphemeType.Affix; }
        }

        public abstract IRule<Word, ShapeNode> CompileAnalysisRule(Morpher morpher);
        public abstract IRule<Word, ShapeNode> CompileSynthesisRule(Morpher morpher);
    }
}
