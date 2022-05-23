using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
    public class AllomorphCoOccurrenceRule : MorphCoOccurrenceRule<Allomorph>
    {
        public AllomorphCoOccurrenceRule(
            ConstraintType type,
            IEnumerable<Allomorph> others,
            MorphCoOccurrenceAdjacency adjacency
        ) : base(type, others, adjacency) { }

        protected override Allomorph GetMorphObject(Allomorph morph)
        {
            return morph;
        }
    }
}
