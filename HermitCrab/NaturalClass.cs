using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class represents a natural class of phonetic segments.
    /// </summary>
    public class NaturalClass : IDBearerBase
    {
        private readonly HashSet<SymbolDefinition> _segDefs;
        private FeatureStruct _featureStruct;
        private FeatureStruct _antiFeatureStruct;

    	/// <summary>
    	/// Initializes a new instance of the <see cref="NaturalClass"/> class.
    	/// </summary>
    	/// <param name="id">The ID.</param>
    	public NaturalClass(string id)
            : base(id)
        {
            _segDefs = new HashSet<SymbolDefinition>();
        }

        /// <summary>
        /// Gets or sets the features.
        /// </summary>
        /// <value>The features.</value>
        public FeatureStruct FeatureStruct
        {
            get
            {
                return _featureStruct;
            }

            set
            {
                _featureStruct = value;
            }
        }

        /// <summary>
        /// Gets or sets the anti features.
        /// </summary>
        /// <value>The anti features.</value>
        public FeatureStruct AntiFeatureStruct
        {
            get
            {
                return _antiFeatureStruct;
            }
        }

        /// <summary>
        /// Gets the segment definitions.
        /// </summary>
        /// <value>The segment definitions.</value>
        public IEnumerable<SymbolDefinition> SegmentDefinitions
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
        public void AddSegmentDefinition(SymbolDefinition segDef)
        {
            _segDefs.Add(segDef);
        }
    }
}
