using System.Collections.Generic;

namespace SIL.APRE
{
	/// <summary>
	/// This represents a left or right word boundary in a phonetic pattern.
	/// </summary>
	public class Margin<TOffset> : PatternNode<TOffset>
	{
		private readonly Direction _marginType;

		/// <summary>
		/// Initializes a new instance of the <see cref="Margin&lt;TOffset&gt;"/> class.
		/// </summary>
		/// <param name="marginType">Type of the margin.</param>
		public Margin(Direction marginType)
		{
			_marginType = marginType;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="margin">The margin.</param>
		public Margin(Margin<TOffset> margin)
		{
			_marginType = margin._marginType;
		}

		/// <summary>
		/// Gets the phonetic sequence node type.
		/// </summary>
		/// <value>The node type.</value>
		public override NodeType Type
		{
			get
			{
				return NodeType.Margin;
			}
		}

		public override IEnumerable<Feature> Features
		{
			get
			{
				yield break;
			}
		}

		/// <summary>
		/// Gets the type of the margin.
		/// </summary>
		/// <value>The type of the margin.</value>
		public Direction MarginType
		{
			get
			{
				return _marginType;
			}
		}

		public override bool IsFeatureReferenced(Feature feature)
		{
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Margin<TOffset>);
		}

		public bool Equals(Margin<TOffset> other)
		{
			if (other == null)
				return false;
			return _marginType == other._marginType;
		}

		public override int GetHashCode()
		{
			return _marginType == Direction.LeftToRight ? 0 : 1;
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Margin<TOffset>(this);
		}

		public override string ToString()
		{
			return "";
		}
	}
}
