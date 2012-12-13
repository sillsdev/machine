using SIL.Machine.FeatureModel;

namespace SIL.Machine.Fsa
{
	public class FstResult<TData, TOffset>
	{
		private readonly TData _output;
		private readonly VariableBindings _varBindings;
		private readonly string _id;
		private readonly int _priority;
		private readonly Annotation<TOffset> _nextAnn;
		private readonly int _index;

		internal FstResult(string id, TData output, VariableBindings varBindings, int priority, Annotation<TOffset> nextAnn, int index)
		{
			_id = id;
			_output = output;
			_varBindings = varBindings;
			_priority = priority;
			_nextAnn = nextAnn;
			_index = index;
		}

		public string ID
		{
			get { return _id; }
		}

		public TData Output
		{
			get { return _output; }
		}

		public VariableBindings VariableBindings
		{
			get { return _varBindings; }
		}

		public int Priority
		{
			get { return _priority; }
		}

		public Annotation<TOffset> NextAnnotation
		{
			get { return _nextAnn; }
		}

		public int Index
		{
			get { return _index; }
		}
	}
}
