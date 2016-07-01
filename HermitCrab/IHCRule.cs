using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	public interface IHCRule
	{
		string Name { get; set; }

		IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher);
		IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher);
	}
}
