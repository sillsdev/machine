using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.FiniteState
{
	internal class NondeterministicFsaTraversalInstance<TData, TOffset> : TraversalInstance<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		private readonly HashSet<State<TData, TOffset>> _visited;

		public NondeterministicFsaTraversalInstance(int registerCount)
			: base(registerCount, false)
		{
			_visited = new HashSet<State<TData, TOffset>>();
		}

		public ISet<State<TData, TOffset>> Visited
		{
			get { return _visited; }
		}

		public override void CopyTo(TraversalInstance<TData, TOffset> other)
		{
			base.CopyTo(other);
			var otherNfsa = (NondeterministicFsaTraversalInstance<TData, TOffset>) other;
			otherNfsa.Visited.UnionWith(_visited);
		}

		public override void Clear()
		{
			base.Clear();
			_visited.Clear();
		}
	}
}