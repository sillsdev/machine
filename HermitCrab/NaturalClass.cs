using System.Collections.Generic;
using SIL.APRE;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class represents a natural class of phonetic segments.
    /// </summary>
    public class NaturalClass : IDBearer
    {
        private readonly HashSet<SegmentDefinition> _segDefs;
        private FeatureStructure _featureStructure;
        private FeatureStructure _antiFeatureStructure;

        /// <summary>
        /// Initializes a new instance of the <see cref="NaturalClass"/> class.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <param name="desc">The description.</param>
        public NaturalClass(string id, string desc)
            : base(id, desc)
        {
            _segDefs = new HashSet<SegmentDefinition>();
        }

        /// <summary>
        /// Gets or sets the features.
        /// </summary>
        /// <value>The features.</value>
        public FeatureStructure FeatureStructure
        {
            get
            {
                return _featureStructure;
            }

            set
            {
                _featureStructure = value;
                _antiFeatureStructure = new SymbolicFeatureValueSet(_featureStructure, true);
            }
        }

        /// <summary>
        /// Gets or sets the anti features.
        /// </summary>
        /// <value>The anti features.</value>
        public FeatureStructure AntiFeatureStructure
        {
            get
            {
                return _antiFeatureStructure;
            }
        }

        /// <summary>
        /// Gets the segment definitions.
        /// </summary>
        /// <value>The segment definitions.</value>
        public IEnumerable<SegmentDefinition> SegmentDefinitions
        {
            get
            {
                return _segDefs;
            }
        }

        /// <summary>
        /// Gets the number of segment definitions.
        /// </summary>
        /// <value>The number of segment definitions.</value>
        public int NumSegmentDefinitions
        {
            get
            {
                return _segDefs.Count;
            }
        }

        /// <summary>
        /// Adds the segment definition.
        /// </summary>
        /// <param name="segDef">The seg def.</param>
        public void AddSegmentDefinition(SegmentDefinition segDef)
        {
            _segDefs.Add(segDef);
        }
    }
}
