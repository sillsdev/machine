using System.Collections.Generic;

namespace SIL.HermitCrab
{
	public class MorphemeCoOccurrenceRule : MorphCoOccurrenceRule<Morpheme>
	{
		public MorphemeCoOccurrenceRule(ConstraintType type, IEnumerable<Morpheme> others, MorphCoOccurrenceAdjacency adjacency)
			: base(type, others, adjacency)
		{
		}

		protected override Morpheme GetMorphObject(Allomorph morph)
		{
			return morph.Morpheme;
		}
	}
}
