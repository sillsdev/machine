using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.Matching;

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

		public override void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IList<Pattern<Word, ShapeNode>> lhs)
		{
			Pattern<Word, ShapeNode> pattern = lhs[_index];
			analysisLhs.Children.Add(new Group<Word, ShapeNode>(_index.ToString(), pattern.Children.Clone()));
		}

		public override void Apply(Match<Word, ShapeNode> match, Word output, Allomorph allomorph)
		{
			GroupCapture<ShapeNode> inputGroup = match[_index.ToString()];
			Span<ShapeNode> outputSpan = match.Input.CopyTo(inputGroup.Span, output);
			if (allomorph != null && Reduplication)
				output.MarkMorph(outputSpan, allomorph);
		}
	}
}
