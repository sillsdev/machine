using System;
using System.Collections.Generic;
using System.Linq;
using SIL.APRE.Fsa;

namespace SIL.APRE.Patterns
{
	/// <summary>
	/// This enumeration represents the node type.
	/// </summary>
	public enum PatternNodeType { Expression, Constraints, Margin, Quantifier, Group, Alternation };

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
			: this(node.Children.Select(child => child.Clone()))
		{
		}

		/// <summary>
		/// Gets the node type.
		/// </summary>
		/// <value>The node type.</value>
		public abstract PatternNodeType Type { get; }

		internal virtual State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState)
		{
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
