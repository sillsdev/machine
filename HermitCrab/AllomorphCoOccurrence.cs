using System.Collections.Generic;

namespace SIL.HermitCrab
{
	public class AllomorphCoOccurrence : MorphCoOccurrence<Allomorph>
	{
		public AllomorphCoOccurrence(IEnumerable<Allomorph> others, MorphCoOccurrenceAdjacency adjacency)
			: base(others, adjacency)
		{
		}

		protected override string GetMorphID(Allomorph morph)
		{
			return morph.ID;
		}
	}
}
