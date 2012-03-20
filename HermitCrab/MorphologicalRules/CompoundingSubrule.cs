using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.MorphologicalRules
{
	public enum Headedness
	{
		LeftHeaded,
		RightHeaded
	}

	public class CompoundingSubrule : IDBearerBase
	{
		private readonly List<Pattern<Word, ShapeNode>> _leftLhs;
		private readonly List<Pattern<Word, ShapeNode>> _rightLhs;

		private readonly List<MorphologicalOutputAction> _leftRhs;
		private readonly List<MorphologicalOutputAction> _rightRhs;

		public CompoundingSubrule(string id)
			: base(id)
		{
			_leftLhs = new List<Pattern<Word, ShapeNode>>();
			_rightLhs = new List<Pattern<Word, ShapeNode>>();
			_leftRhs = new List<MorphologicalOutputAction>();
			_rightRhs = new List<MorphologicalOutputAction>();
		}

		public Headedness Headedness { get; set; }

		public IList<Pattern<Word, ShapeNode>> LeftLhs
		{
			get { return _leftLhs; }
		}

		public IList<Pattern<Word, ShapeNode>> RightLhs
		{
			get { return _rightLhs; }
		}

		public IList<MorphologicalOutputAction> LeftRhs
		{
			get { return _leftRhs; }
		}

		public IList<MorphologicalOutputAction> RightRhs
		{
			get { return _rightRhs; }
		}
	}
}
