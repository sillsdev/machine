using System.Collections.Generic;
using System.Linq;
using SIL.APRE.Fsa;

namespace SIL.APRE
{
    /// <summary>
    /// This class represents a simple context in a phonetic pattern. Simple contexts are used to represent
    /// natural classes and segments in a pattern.
    /// </summary>
    public class Constraints<TOffset> : PatternNode<TOffset>
    {
    	private readonly FeatureStructure _fs;
    	private readonly IDictionary<string, bool> _variables;

        /// <summary>
		/// Initializes a new instance of the <see cref="Constraints{TOffset}"/> class.
        /// </summary>
		public Constraints(FeatureStructure fs, IDictionary<string, bool> variables)
        {
            _fs = fs;
			_variables = variables;
        }

		public Constraints(FeatureStructure fs)
			: this(fs, null)
		{
		}

    	/// <summary>
    	/// Copy constructor.
    	/// </summary>
    	/// <param name="constraints">The annotation constraints.</param>
    	public Constraints(Constraints<TOffset> constraints)
        {
            _fs = (FeatureStructure) constraints._fs.Clone();

			if (constraints._variables != null)
				_variables = new Dictionary<string, bool>(constraints._variables);
        }

        /// <summary>
        /// Gets the node type.
        /// </summary>
        /// <value>The node type.</value>
        public override NodeType Type
        {
            get
            {
                return NodeType.Constraints;
            }
        }

        /// <summary>
        /// Gets the features.
        /// </summary>
        /// <value>The features.</value>
        public override IEnumerable<Feature> Features
        {
            get
            {
				return new HashSet<Feature>(_fs.Features);
            }
        }

		/// <summary>
		/// Determines whether this node references the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>
		/// 	<c>true</c> if the specified feature is referenced, otherwise <c>false</c>.
		/// </returns>
		public override bool IsFeatureReferenced(Feature feature)
		{
			return Features.Contains(feature);
		}

        /// <summary>
        /// Gets the feature values.
        /// </summary>
        /// <value>The feature values.</value>
        public FeatureStructure FeatureStructure
        {
            get
            {
                return _fs;
            }
        }

    	public IDictionary<string, bool> Variables
    	{
    		get { return _variables; }
    	}

    	internal override State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState)
		{
			State<TOffset> endState = fsa.CreateState();
    		var fs = (FeatureStructure) _fs.Clone();
    		startState.AddArc(new Arc<TOffset>(new ArcCondition<TOffset>(fs),
				endState));

			return base.GenerateNfa(fsa, endState);
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Constraints<TOffset>(this);
		}

		public override int GetHashCode()
		{
			return _fs.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Constraints<TOffset>);
		}

		public bool Equals(Constraints<TOffset> other)
		{
			if (other == null)
				return false;

			return _fs.Equals(other._fs);
		}

		public override string ToString()
		{
			return string.Format("[{0}]", _fs);
		}
    }
}
