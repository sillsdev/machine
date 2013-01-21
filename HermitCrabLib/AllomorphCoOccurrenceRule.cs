using System.Collections.Generic;

namespace SIL.HermitCrab
{
	public class AllomorphCoOccurrenceRule : MorphCoOccurrenceRule<Allomorph>
	{
		public AllomorphCoOccurrenceRule(IEnumerable<Allomorph> others, MorphCoOccurrenceAdjacency adjacency)
			: base(others, adjacency)
		{
		}

		protected override string GetMorphID(Allomorph morph)
		{
			return morph.ID;
		}
	}
}
