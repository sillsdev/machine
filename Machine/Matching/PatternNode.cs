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
			: base(begin => new TempPatternNode())
		{
		}

		protected PatternNode(IEnumerable<PatternNode<TData, TOffset>> children)
			: this()
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
			get { return this.Root() as Pattern<TData, TOffset>; }
		}

		internal virtual State<TData, TOffset> GenerateNfa(FiniteStateAutomaton<TData, TOffset> fsa, State<TData, TOffset> startState)
		{
			if (this.IsLeaf())
				return startState;

			foreach (PatternNode<TData, TOffset> child in Children.GetNodes(fsa.Direction))
				startState = child.GenerateNfa(fsa, startState);

			return startState;
		}

		public abstract PatternNode<TData, TOffset> Clone();

		private class TempPatternNode : PatternNode<TData, TOffset>
		{
			public override PatternNode<TData, TOffset> Clone()
			{
				return new TempPatternNode();
			}

			protected override bool CanAdd(PatternNode<TData,TOffset> child)
			{
				return false;
			}
		}
	}
}
