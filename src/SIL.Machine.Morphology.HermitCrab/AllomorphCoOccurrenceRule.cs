using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
	public class AllomorphCoOccurrenceRule : MorphCoOccurrenceRule<Allomorph>
	{
		public AllomorphCoOccurrenceRule(IEnumerable<Allomorph> others, MorphCoOccurrenceAdjacency adjacency)
			: base(others, adjacency)
		{
		}

		protected override Allomorph GetMorphObject(Allomorph morph)
		{
			return morph;
		}
	}
}
