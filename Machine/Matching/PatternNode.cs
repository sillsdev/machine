using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Fsa;

namespace SIL.Machine.Matching
{
	/// <summary>
	/// This is the abstract class that all phonetic pattern nodes extend.
	/// </summary>
	public abstract class PatternNode<TData, TOffset> : OrderedBidirTreeNode<PatternNode<TData, TOffset>>, IDeepCloneable<PatternNode<TData, TOffset>> where TData : IData<TOffset>
	{
		protected PatternNode()
			: base(begin => new Margin())
		{
		}

		protected PatternNode(IEnumerable<PatternNode<TData, TOffset>> children)
			: this()
		{
			foreach (PatternNode<TData, TOffset> child in children)
				Children.Add(child);
		}

		protected PatternNode(PatternNode<TData, TOffset> node)
			: this(node.Children.DeepClone())
		{
		}

		protected Pattern<TData, TOffset> Pattern
		{
			get { return Root as Pattern<TData, TOffset>; }
		}

		internal virtual State<TData, TOffset> GenerateNfa(FiniteStateAutomaton<TData, TOffset> fsa, State<TData, TOffset> startState)
		{
			if (IsLeaf)
				return startState;

			foreach (PatternNode<TData, TOffset> child in Children.GetNodes(fsa.Direction))
				startState = child.GenerateNfa(fsa, startState);

			return startState;
		}

		public PatternNode<TData, TOffset> DeepClone()
		{
			return DeepCloneImpl();
		}

		protected abstract PatternNode<TData, TOffset> DeepCloneImpl();

		private class Margin : PatternNode<TData, TOffset>
		{
			protected override PatternNode<TData, TOffset> DeepCloneImpl()
			{
				return new Margin();
			}

			protected override bool CanAdd(PatternNode<TData,TOffset> child)
			{
				return false;
			}
		}
	}
}
