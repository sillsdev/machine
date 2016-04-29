using System.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.HermitCrab
{
	public interface IHCRule
	{
		string Name { get; set; }

		IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher);
		IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher);

		IDictionary Properties { get; }
	}
}
