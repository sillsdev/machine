using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class CompoundingSubrule : IDBearerBase
	{
		private readonly List<Pattern<Word, ShapeNode>> _headLhs;
		private readonly List<Pattern<Word, ShapeNode>> _nonHeadLhs;

		private readonly List<MorphologicalOutputAction> _rhs;

		public CompoundingSubrule(string id)
			: base(id)
		{
			_headLhs = new List<Pattern<Word, ShapeNode>>();
			_nonHeadLhs = new List<Pattern<Word, ShapeNode>>();
			_rhs = new List<MorphologicalOutputAction>();
		}

		public IList<Pattern<Word, ShapeNode>> HeadLhs
		{
			get { return _headLhs; }
		}

		public IList<Pattern<Word, ShapeNode>> NonHeadLhs
		{
			get { return _nonHeadLhs; }
		}

		public IList<MorphologicalOutputAction> Rhs
		{
			get { return _rhs; }
		}
	}
}
