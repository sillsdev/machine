using System.Linq;
using SIL.APRE.FeatureModel;
using SIL.APRE.Fsa;

namespace SIL.APRE.Matching
{
    /// <summary>
    /// This class represents a simple context in a phonetic pattern. Simple contexts are used to represent
    /// natural classes and segments in a pattern.
    /// </summary>
	public class Constraint<TData, TOffset> : PatternNode<TData, TOffset> where TData : IData<TOffset>
    {
    	private readonly FeatureStruct _fs;

        /// <summary>
		/// Initializes a new instance of the <see cref="Constraint{TData, TOffset}"/> class.
        /// </summary>
		public Constraint(string type, FeatureStruct fs)
		{
			_fs = fs;
			_fs.AddValue(AnnotationFeatureSystem.Type, type);
		}

    	/// <summary>
    	/// Copy constructor.
    	/// </summary>
    	/// <param name="constraint">The annotation constraints.</param>
		public Constraint(Constraint<TData, TOffset> constraint)
        {
            _fs = (FeatureStruct) constraint._fs.Clone();
        }

    	public string Type
    	{
    		get { return _fs.GetValue<StringFeatureValue>(AnnotationFeatureSystem.Type).Values.First(); }
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

		internal override State<TData, TOffset> GenerateNfa(FiniteStateAutomaton<TData, TOffset> fsa, State<TData, TOffset> startState)
		{
    		return startState.AddArc((FeatureStruct) _fs.Clone(), fsa.CreateState());
		}

		public override PatternNode<TData, TOffset> Clone()
		{
			return new Constraint<TData, TOffset>(this);
		}

		public override int GetHashCode()
		{
			return _fs.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Constraint<TData, TOffset>);
		}

		public bool Equals(Constraint<TData, TOffset> other)
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
