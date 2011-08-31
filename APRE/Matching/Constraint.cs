using System.Collections.Generic;
using SIL.APRE.FeatureModel;
using SIL.APRE.Fsa;

namespace SIL.APRE.Matching
{
    /// <summary>
    /// This class represents a simple context in a phonetic pattern. Simple contexts are used to represent
    /// natural classes and segments in a pattern.
    /// </summary>
    public class Constraint<TOffset> : PatternNode<TOffset>
    {
    	private readonly FeatureStruct _fs;
    	private readonly IDictionary<string, bool> _variables;

        /// <summary>
		/// Initializes a new instance of the <see cref="Constraint{TOffset}"/> class.
        /// </summary>
		public Constraint(FeatureStruct fs, IDictionary<string, bool> variables)
        {
            _fs = fs;
			_variables = variables;
        }

		public Constraint(FeatureStruct fs)
			: this(fs, null)
		{
		}

    	/// <summary>
    	/// Copy constructor.
    	/// </summary>
    	/// <param name="constraint">The annotation constraints.</param>
    	public Constraint(Constraint<TOffset> constraint)
        {
            _fs = (FeatureStruct) constraint._fs.Clone();

			if (constraint._variables != null)
				_variables = new Dictionary<string, bool>(constraint._variables);
        }

        /// <summary>
        /// Gets the node type.
        /// </summary>
        /// <value>The node type.</value>
        public override PatternNodeType Type
        {
            get
            {
                return PatternNodeType.Constraint;
            }
        }

        /// <summary>
        /// Gets the feature values.
        /// </summary>
        /// <value>The feature values.</value>
        public FeatureStruct FeatureStruct
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
    		var fs = (FeatureStruct) _fs.Clone();
    		return startState.AddArc(new Arc<TOffset>(new ArcCondition<TOffset>(fs), fsa.CreateState()));
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Constraint<TOffset>(this);
		}

		public override int GetHashCode()
		{
			return _fs.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Constraint<TOffset>);
		}

		public bool Equals(Constraint<TOffset> other)
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
