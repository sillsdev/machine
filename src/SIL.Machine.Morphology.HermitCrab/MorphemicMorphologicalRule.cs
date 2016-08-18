using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
	public abstract class MorphemicMorphologicalRule : Morpheme, IMorphologicalRule
	{
		public string Name { get; set; }
		public bool IsTemplateRule { get; set; }

		public abstract IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher);
		public abstract IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher);
	}
}
