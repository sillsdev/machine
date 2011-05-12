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
    public class AnnotationConstraints<TOffset> : PatternNode<TOffset>
    {
    	private readonly string _annotationType;
    	private readonly FeatureStructure _featureStructure;
    	private readonly IDictionary<string, bool> _variables;
    	private readonly AlphaVariables<TOffset> _alphaVars;

        /// <summary>
		/// Initializes a new instance of the <see cref="AnnotationConstraints&lt;TOffset&gt;"/> class.
        /// </summary>
		public AnnotationConstraints(string annotationType, FeatureStructure featureStructure,
			IDictionary<string, bool> variables, AlphaVariables<TOffset> alphaVars)
        {
			_annotationType = annotationType;
            _featureStructure = featureStructure;
			_variables = variables;
			_alphaVars = alphaVars;
        }

		public AnnotationConstraints(string annotationType, FeatureStructure featureStructure)
			: this(annotationType, featureStructure, null, null)
		{
		}

    	/// <summary>
    	/// Copy constructor.
    	/// </summary>
    	/// <param name="constraints">The annotation constraints.</param>
    	public AnnotationConstraints(AnnotationConstraints<TOffset> constraints)
        {
			_annotationType = constraints._annotationType;
            _featureStructure = constraints._featureStructure.Clone() as FeatureStructure;

			if (constraints._variables != null)
				_variables = new Dictionary<string, bool>(constraints._variables);
			_alphaVars = constraints._alphaVars;
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
                var features = new HashSet<Feature>(_featureStructure.Features);
				if (_variables != null)
				{
					// get features from variables
					foreach (string variable in _variables.Keys)
					{
						IEnumerable<Feature> path = _alphaVars.GetFeaturePath(variable);
						features.Add(path.Last());
					}
				}
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
                return _featureStructure;
            }
        }

    	public IDictionary<string, bool> Variables
    	{
    		get { return _variables; }
    	}

    	/// <summary>
    	/// Checks if the specified phonetic shape node matches this simple context.
    	/// </summary>
    	/// <param name="ann">The annotation.</param>
    	/// <param name="mode">The mode.</param>
    	/// <param name="varValues">The instantiated variables.</param>
    	/// <returns>All matches.</returns>
    	public override bool IsMatch(Annotation<TOffset> ann, ModeType mode, ref FeatureStructure varValues)
    	{
			if (varValues != null)
    			varValues = (FeatureStructure) varValues.Clone();

			if (ann == null)
				return false;

			if (_annotationType == ann.Type)
			{
				if (Pattern != null && Pattern.CheckClean(mode))
				{
					// check segment to see if it has already been altered by another
					// subrule, only matters for simultaneously applying rules
					if (!ann.IsClean)
						return false;
				}

				if (!IsFeatureMatch(ann, mode, varValues))
					return false;

				return true;
			}
			return false;
        }

		internal override State<TOffset, FeatureStructure> GenerateNfa(FiniteStateAutomaton<TOffset, FeatureStructure> fsa,
			State<TOffset, FeatureStructure> startState, Direction dir)
		{
			State<TOffset, FeatureStructure> endState = fsa.CreateState();
			startState.AddTransition(new Transition<TOffset, FeatureStructure>(this, endState));

			return base.GenerateNfa(fsa, endState, dir);
		}

        protected virtual bool IsFeatureMatch(Annotation<TOffset> ann, ModeType mode, FeatureStructure varValues)
        {
            // check unifiability
            if (!ann.FeatureStructure.IsUnifiable(_featureStructure))
                return false;

			if (_alphaVars != null && varValues != null)
			{
				// only one possible binding during synthesis
				if (mode == ModeType.Synthesis)
				{
					if (!_alphaVars.GetBinding(_variables, ann, varValues))
					{
						// when a variable is specified in a target and environment for agreement, the environment
						// must specify a feature for each variable
						if (_variables.Any(varPolarity => !_alphaVars.GetBinding(varPolarity.Key, varPolarity.Value, ann, null)))
							throw new Exception();
						return false;
					}
				}
				else
				{
					// during analysis, get all possible bindings, since a feature could
					// be uninstantiated
					if (!_alphaVars.GetAllBindings(_variables, ann, varValues))
						return false;
				}
			}

            return true;
        }

		public void InstantiateVariables(Annotation<TOffset> ann, FeatureStructure varValues)
		{
			_alphaVars.Instantiate(ann, _variables, varValues);
		}

		public void UninstantiateVariables(Annotation<TOffset> ann, FeatureStructure varValues)
		{
			_alphaVars.Uninstantiate(ann, _variables, varValues);
		}

		public override PatternNode<TOffset> Clone()
		{
			return new AnnotationConstraints<TOffset>(this);
		}

		public override int GetHashCode()
		{
			return _annotationType.GetHashCode() ^ _featureStructure.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as AnnotationConstraints<TOffset>);
		}

		public bool Equals(AnnotationConstraints<TOffset> other)
		{
			if (other == null)
				return false;
			return _annotationType == other._annotationType && _featureStructure.Equals(other._featureStructure);
		}

		public override string ToString()
		{
			return string.Format("[{0}]", _annotationType);
		}
    }
}
