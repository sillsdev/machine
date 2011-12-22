using System.Collections.Generic;
using SIL.Machine.Fsa;

namespace SIL.Machine.Matching
{
	/// <summary>
	/// This is the abstract class that all phonetic pattern nodes extend.
	/// </summary>
	public abstract class PatternNode<TData, TOffset> : OrderedBidirTreeNode<PatternNode<TData, TOffset>>, ICloneable<PatternNode<TData, TOffset>> where TData : IData<TOffset>
	{
		protected PatternNode()
		{
		}

		protected PatternNode(IEnumerable<PatternNode<TData, TOffset>> children)
		{
			foreach (PatternNode<TData, TOffset> child in children)
				Children.Add(child);
		}

		protected PatternNode(PatternNode<TData, TOffset> node)
			: this(node.Children.Clone())
		{
		}

		protected Pattern<TData, TOffset> Pattern
		{
			get { return this.GetRoot() as Pattern<TData, TOffset>; }
		}

		internal virtual State<TData, TOffset> GenerateNfa(FiniteStateAutomaton<TData, TOffset> fsa, State<TData, TOffset> startState)
		{
			if (IsLeaf)
				return startState;

			foreach (PatternNode<TData, TOffset> child in Children.GetNodes(fsa.Direction))
				startState = child.GenerateNfa(fsa, startState);

			return startState;
		}

		public abstract PatternNode<TData, TOffset> Clone();
	}
}
