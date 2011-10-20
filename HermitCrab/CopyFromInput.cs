using System.Collections.Generic;
using SIL.APRE;
using SIL.APRE.Matching;

namespace SIL.HermitCrab
{
	public class CopyFromInput : MorphologicalOutput
	{
		private readonly int _index;

		public CopyFromInput(int index)
		{
			_index = index;
		}

		public int Index
		{
			get { return _index; }
		}

		public override void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IList<Expression<Word, ShapeNode>> lhs)
		{
			Expression<Word, ShapeNode> expr = lhs[_index];
			analysisLhs.Children.Add(new Group<Word, ShapeNode>(_index.ToString(), expr.Children.Clone()));
		}

		public override void Apply(PatternMatch<ShapeNode> match, Word input, Word output, Allomorph allomorph)
		{
			Span<ShapeNode> inputSpan = match[_index.ToString()];
			Span<ShapeNode> outputSpan = input.CopyTo(inputSpan, output);
			AddMorphAnnotation(output, outputSpan.Start, outputSpan.End, allomorph);
		}
	}
}
