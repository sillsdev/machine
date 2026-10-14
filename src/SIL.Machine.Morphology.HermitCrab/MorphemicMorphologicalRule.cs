using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    public abstract class MorphemicMorphologicalRule : Morpheme, IMorphologicalRule
    {
        public string Name { get; set; }
        public bool IsTemplateRule { get; set; }
        /// <summary>
        /// If IsCliticRule is not set, GetIsCliticRule() will return whether the rule is in the "Clitics" stratum.
        /// </summary>
        public bool? IsCliticRule { get; set; }
        internal bool GetIsCliticRule()
        {
            if (IsCliticRule == null)
                return Stratum != null && Stratum.Name == "Clitics";
            return (bool)IsCliticRule;
        }

        public override MorphemeType MorphemeType
        {
            get { return MorphemeType.Affix; }
        }

        public abstract IRule<Word, ShapeNode> CompileAnalysisRule(Morpher morpher);
        public abstract IRule<Word, ShapeNode> CompileSynthesisRule(Morpher morpher);
    }
}
