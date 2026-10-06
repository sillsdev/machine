using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    public abstract class MorphemicMorphologicalRule : Morpheme, IMorphologicalRule
    {
        private bool? _isCliticRule;
        public string Name { get; set; }
        public bool IsTemplateRule { get; set; }
        public bool IsCliticRule
        {
            get
            {
                if (_isCliticRule == null)
                    _isCliticRule = Stratum != null && Stratum.Name == "Clitics";
                return (bool)_isCliticRule;
            }
            set { _isCliticRule = value; }
        }

        public override MorphemeType MorphemeType
        {
            get { return MorphemeType.Affix; }
        }

        public abstract IRule<Word, ShapeNode> CompileAnalysisRule(Morpher morpher);
        public abstract IRule<Word, ShapeNode> CompileSynthesisRule(Morpher morpher);
    }
}
