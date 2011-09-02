using System;
using System.Collections.Generic;
using SIL.APRE.Fsa;

namespace SIL.APRE.Matching
{
	/// <summary>
	/// This is the abstract class that all phonetic pattern nodes extend.
	/// </summary>
	public abstract class PatternNode<TOffset> : BidirTreeNode<PatternNode<TOffset>>, ICloneable
	{
		protected PatternNode()
		{
		}

		protected PatternNode(IEnumerable<PatternNode<TOffset>> children)
		{
			foreach (PatternNode<TOffset> child in children)
				Children.Add(child);
		}

		protected PatternNode(PatternNode<TOffset> node)
			: this(node.Children.Clone())
		{
		}

		protected Pattern<TOffset> Pattern
		{
			get { return this.GetRoot() as Pattern<TOffset>; }
		}

		internal virtual State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState)
		{
			if (IsLeaf)
				return startState;

			foreach (PatternNode<TOffset> child in Children.GetNodes(fsa.Direction))
				startState = child.GenerateNfa(fsa, startState);

			return startState;
		}

		public abstract PatternNode<TOffset> Clone();

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
