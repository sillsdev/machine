using SIL.Machine.Annotations;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// This class represents an allomorph in a lexical entry.
    /// </summary>
    public class RootAllomorph : Allomorph
    {
        private readonly Segments _segments;


        /// <summary>
        /// Initializes a new instance of the <see cref="RootAllomorph"/> class.
        /// </summary>
        public RootAllomorph(Segments segments)
        {
            _segments = segments;
            foreach (ShapeNode node in _segments.Shape.GetNodes(_segments.Shape.Range))
            {
                if (
                    node.Iterative
                    || (node.Annotation.Optional && node.Annotation.Type() != HCFeatureSystem.Boundary)
                )
                {
                    IsPattern = true;
                }
            }

        }

        /// <summary>
        /// Gets the segments.
        /// </summary>
        public Segments Segments
        {
            get { return _segments; }
        }

        public StemName StemName { get; set; }

        public bool IsBound { get; set; }

        /// <summary>
        /// Does this represent a lexical pattern (e.g. [Seg]*)?
        /// </summary>
        public bool IsPattern
        {
            get; private set;
        }

        protected override bool ConstraintsEqual(Allomorph other)
        {
            if (!(other is RootAllomorph otherAllo))
                return false;

            return base.ConstraintsEqual(other) && IsBound == otherAllo.IsBound;
        }

        protected override bool CheckAllomorphConstraints(Morpher morpher, Allomorph allomorph, Word word)
        {
            if (IsBound && word.Allomorphs.Count == 1)
            {
                if (morpher != null && morpher.TraceManager.IsTracing)
                    morpher.TraceManager.Failed(morpher.Language, word, FailureReason.BoundRoot, this, null);
                return false;
            }

            if (StemName != null && !StemName.IsRequiredMatch(word.SyntacticFeatureStruct))
            {
                if (morpher != null && morpher.TraceManager.IsTracing)
                    morpher.TraceManager.Failed(morpher.Language, word, FailureReason.RequiredStemName, this, StemName);
                return false;
            }

            foreach (
                RootAllomorph otherAllo in ((LexEntry)Morpheme).Allomorphs.Where(a => a != this && a.StemName != null)
            )
            {
                if (!otherAllo.StemName.IsExcludedMatch(word.SyntacticFeatureStruct, StemName))
                {
                    if (morpher != null && morpher.TraceManager.IsTracing)
                    {
                        morpher.TraceManager.Failed(
                            morpher.Language,
                            word,
                            FailureReason.ExcludedStemName,
                            this,
                            otherAllo.StemName
                        );
                    }

                    return false;
                }
            }

            return base.CheckAllomorphConstraints(morpher, allomorph, word);
        }
    }
}
