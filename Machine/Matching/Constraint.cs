using SIL.Collections;
using SIL.Machine.FeatureModel;
using SIL.Machine.Fsa;

namespace SIL.Machine.Matching
{
    /// <summary>
    /// This class represents a simple context in a phonetic pattern. Simple contexts are used to represent
    /// natural classes and segments in a pattern.
    /// </summary>
	public class Constraint<TData, TOffset> : PatternNode<TData, TOffset>, IDeepCloneable<Constraint<TData, TOffset>> where TData : IData<TOffset>
    {
    	private readonly FeatureStruct _fs;

        /// <summary>
		/// Initializes a new instance of the <see cref="Constraint{TData, TOffset}"/> class.
        /// </summary>
		public Constraint(FeatureStruct fs)
		{
			_fs = fs;
		}

    	/// <summary>
    	/// Copy constructor.
    	/// </summary>
    	/// <param name="constraint">The annotation constraints.</param>
		protected Constraint(Constraint<TData, TOffset> constraint)
        {
            _fs = constraint._fs.DeepClone();
        }

        /// <summary>
        /// Gets the feature values.
        /// </summary>
        /// <value>The feature values.</value>
        public FeatureStruct FeatureStruct
        {
            get { return _fs; }
        }

		protected override bool CanAdd(PatternNode<TData, TOffset> child)
		{
			return false;
		}

		internal override State<TData, TOffset> GenerateNfa(FiniteStateAutomaton<TData, TOffset> fsa, State<TData, TOffset> startState, out bool hasVariables)
		{
			hasVariables = _fs.HasVariables;
    		return startState.AddArc(_fs.DeepClone(), fsa.CreateState());
		}

		protected override PatternNode<TData, TOffset> DeepCloneImpl()
		{
			return DeepClone();
		}

    	public new Constraint<TData, TOffset> DeepClone()
    	{
			return new Constraint<TData, TOffset>(this);
    	}

    	public override string ToString()
		{
			return _fs.ToString();
		}
    }
}
