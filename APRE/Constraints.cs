using System;
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
    	private readonly string _annotationType;
    	private readonly FeatureStructure _fs;
    	private readonly IDictionary<string, bool> _variables;

        /// <summary>
		/// Initializes a new instance of the <see cref="Constraints{TOffset}"/> class.
        /// </summary>
		public Constraints(string annotationType, FeatureStructure fs,
			IDictionary<string, bool> variables)
        {
			_annotationType = annotationType;
            _fs = fs;
			_variables = variables;
        }

		public Constraints(string annotationType, FeatureStructure _fs)
			: this(annotationType, _fs, null)
		{
		}

		public Constraints(string annotationType)
			: this(annotationType, null)
		{
		}

    	/// <summary>
    	/// Copy constructor.
    	/// </summary>
    	/// <param name="constraints">The annotation constraints.</param>
    	public Constraints(Constraints<TOffset> constraints)
        {
			_annotationType = constraints._annotationType;
            _fs = constraints._fs == null ? null : (FeatureStructure) constraints._fs.Clone();

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

		public string AnnotationType
		{
			get
			{
				return _annotationType;
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
				if (_fs == null)
					return Enumerable.Empty<Feature>();

                var features = new HashSet<Feature>(_fs.Features);
				return features;
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

    	internal override State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa,
			State<TOffset> startState, int varValueIndex, IEnumerable<Tuple<string, IEnumerable<Feature>, FeatureSymbol>> varValues)
		{
			State<TOffset> endState = fsa.CreateState();
    		var fs = (FeatureStructure) _fs.Clone();
			if (_variables != null && _variables.Any())
			{
				foreach (Tuple<string, IEnumerable<Feature>, FeatureSymbol> varValue in varValues)
				{
					bool agree;
					if (_variables.TryGetValue(varValue.Item1, out agree))
					{
						SymbolicFeatureValue fv;
						if (agree)
						{
							fv = new SymbolicFeatureValue(varValue.Item3);
						}
						else
						{
							var sf = (SymbolicFeature) varValue.Item2.Last();
							fv = new SymbolicFeatureValue(sf.PossibleSymbols.Where(symbol => symbol != varValue.Item3));
						}
						fs.Add(varValue.Item2, fv);
					}
				}
			}
    		startState.AddArc(new Arc<TOffset>(new AnnotationArcCondition<TOffset>(_annotationType, fs),
				endState));

			return base.GenerateNfa(fsa, endState, varValueIndex, varValues);
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Constraints<TOffset>(this);
		}

		public override int GetHashCode()
		{
			return _annotationType.GetHashCode() ^ (_fs == null ? 0 : _fs.GetHashCode());
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

			if (_fs == null)
			{
				if (other._fs != null)
					return false;
			}
			else if (!_fs.Equals(other._fs))
			{
				return false;
			}

			return _annotationType == other._annotationType;
		}

		public override string ToString()
		{
			return string.Format("[{0}]", _annotationType);
		}
    }
}
