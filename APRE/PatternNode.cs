using System;
using System.Collections.Generic;
using SIL.APRE.Fsa;

namespace SIL.APRE
{
	/// <summary>
	/// This is the abstract class that all phonetic pattern nodes extend.
	/// </summary>
	public abstract class PatternNode<TOffset> : BidirListNode<PatternNode<TOffset>>, ITransitionCondition<TOffset, FeatureStructure>, ICloneable
	{
		/// <summary>
		/// This enumeration represents the node type.
		/// </summary>
		public enum NodeType { Constraints, Margin, Range, Pattern };

		private readonly int _groupNum;

		protected PatternNode()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PatternNode&lt;TOffset&gt;"/> class.
		/// </summary>
		protected PatternNode(int groupNum)
		{
			_groupNum = groupNum;
		}

		protected PatternNode(PatternNode<TOffset> node)
		{
			_groupNum = node.GroupNum;
		}

		public int GroupNum
		{
			get { return _groupNum; }
		}

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

		public virtual bool IsMatch(Annotation<TOffset> ann, ModeType mode, ref FeatureStructure varValues)
		{
			varValues = (FeatureStructure) varValues.Clone();

			return true;
		}

		internal virtual State<TOffset, FeatureStructure> GenerateNfa(FiniteStateAutomaton<TOffset, FeatureStructure> fsa,
			State<TOffset, FeatureStructure> startState, Direction dir)
		{
			PatternNode<TOffset> nextNode = GetNext(dir);
			if (nextNode == null)
				return startState;

			return nextNode.GenerateNfa(fsa, startState, dir);
		}

		public abstract PatternNode<TOffset> Clone();

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
