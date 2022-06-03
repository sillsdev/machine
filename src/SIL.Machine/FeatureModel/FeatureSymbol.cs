using SIL.Machine.DataStructures;

namespace SIL.Machine.FeatureModel
{
    /// <summary>
    /// This class represents a feature value.
    /// </summary>
    public class FeatureSymbol : IDBearerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureSymbol"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        public FeatureSymbol(string id) : base(id)
        {
            Index = -1;
        }

        public FeatureSymbol(string id, string desc) : this(id)
        {
            Description = desc;
        }

        /// <summary>
        /// Gets or sets the feature.
        /// </summary>
        /// <value>The feature.</value>
        public SymbolicFeature Feature { get; internal set; }

        internal int Index { get; set; }
    }
}
