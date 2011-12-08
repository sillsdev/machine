using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.Matching;

namespace SIL.HermitCrab
{
	public class AffixProcessAllomorph : Allomorph
	{
		private readonly List<Pattern<Word, ShapeNode>> _lhs;
		private readonly List<MorphologicalOutput> _rhs;

		public AffixProcessAllomorph(string id)
			: base(id)
		{
			_lhs = new List<Pattern<Word, ShapeNode>>();
			_rhs = new List<MorphologicalOutput>();
		}

		public IList<Pattern<Word, ShapeNode>> Lhs
		{
			get { return _lhs; }
		}

		public IList<MorphologicalOutput> Rhs
		{
			get { return _rhs; }
		}

	}
}
