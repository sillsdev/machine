using System.Collections.Generic;
using SIL.Machine;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class represents a morpheme. All morpheme objects should extend this class.
    /// </summary>
    public abstract class Morpheme : IDBearerBase
    {
        private Stratum _stratum;
        private Gloss _gloss;
        private IEnumerable<MorphCoOccurrence> _requiredMorphCoOccur;
        private IEnumerable<MorphCoOccurrence> _excludedMorphCoOccur;

    	/// <summary>
    	/// Initializes a new instance of the <see cref="Morpheme"/> class.
    	/// </summary>
    	/// <param name="id">The id.</param>
    	protected Morpheme(string id)
            : base(id)
        {
        }

        /// <summary>
        /// Gets or sets the stratum.
        /// </summary>
        /// <value>The stratum.</value>
        public Stratum Stratum
        {
            get
            {
                return _stratum;
            }

            internal set
            {
                _stratum = value;
            }
        }

        /// <summary>
        /// Gets or sets the morpheme's gloss.
        /// </summary>
        /// <value>The gloss.</value>
        public Gloss Gloss
        {
            get
            {
                return _gloss;
            }

            set
            {
                _gloss = value;
            }
        }

        /// <summary>
        /// Gets or sets the required morpheme co-occurrences.
        /// </summary>
        /// <value>The required morpheme co-occurrences.</value>
        public IEnumerable<MorphCoOccurrence> RequiredMorphemeCoOccurrences
        {
            get
            {
                return _requiredMorphCoOccur;
            }

            set
            {
                _requiredMorphCoOccur = value;
            }
        }

        /// <summary>
        /// Gets or sets the excluded morpheme co-occurrences.
        /// </summary>
        /// <value>The excluded morpheme co-occurrences.</value>
        public IEnumerable<MorphCoOccurrence> ExcludedMorphemeCoOccurrences
        {
            get
            {
                return _excludedMorphCoOccur;
            }

            set
            {
                _excludedMorphCoOccur = value;
            }
        }
    }
}