using System;
using System.Collections.Generic;
using SIL.APRE.Fsa;

namespace SIL.APRE
{
	/// <summary>
	/// This is the abstract class that all phonetic pattern nodes extend.
	/// </summary>
	public abstract class PatternNode<TOffset> : BidirListNode<PatternNode<TOffset>>, ICloneable
	{
		/// <summary>
		/// This enumeration represents the node type.
		/// </summary>
		public enum NodeType { Constraints, Margin, Quantifier, Group, Alternation };

		/// <summary>
		/// Gets the node type.
		/// </summary>
		/// <value>The node type.</value>
		public abstract NodeType Type { get; }

		/// <summary>
		/// Gets the features.
		/// </summary>
		/// <value>The features.</value>
		public abstract IEnumerable<Feature> Features { get; }

		/// <summary>
		/// Determines whether this node references the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>
		/// 	<c>true</c> if the specified feature is referenced, otherwise <c>false</c>.
		/// </returns>
		public abstract bool IsFeatureReferenced(Feature feature);

		public virtual Pattern<TOffset> Pattern { get; internal set; }

		internal virtual State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa,
			State<TOffset> startState, int varValueIndex, IEnumerable<Tuple<string, IEnumerable<Feature>, FeatureSymbol>> varValues)
		{
			PatternNode<TOffset> nextNode = GetNext(fsa.Direction);
			if (nextNode == null)
				return startState;

			return nextNode.GenerateNfa(fsa, startState, varValueIndex, varValues);
		}

		public abstract PatternNode<TOffset> Clone();

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
