using System;
using System.Globalization;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine
{
	public class ShapeNode : OrderedBidirListNode<ShapeNode>, IComparable<ShapeNode>, IComparable, IDeepCloneable<ShapeNode>, IFreezable<ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory; 
		private readonly Annotation<ShapeNode> _ann;

		public ShapeNode(SpanFactory<ShapeNode> spanFactory, FeatureStruct fs)
		{
			_spanFactory = spanFactory;
			_ann = new Annotation<ShapeNode>(spanFactory.Create(this), fs);
			Tag = int.MinValue;
		}

		protected ShapeNode(ShapeNode node)
			: this(node._spanFactory, node.Annotation.FeatureStruct.DeepClone())
		{
			_ann.Optional = node.Annotation.Optional;
		}

		public int Tag { get; internal set; }

		public Annotation<ShapeNode> Annotation
		{
			get { return _ann; }
		}

		public bool IsFrozen { get; private set; }

		public void Freeze()
		{
			if (IsFrozen)
				return;
			IsFrozen = true;
		}

		public bool ValueEquals(ShapeNode other)
		{
			if (other == null)
				return false;

			return Tag == other.Tag;
		}

		public int GetFrozenHashCode()
		{
			if (!IsFrozen)
				throw new InvalidOperationException("The shape node does not have a valid hash code, because it is mutable.");
			return Tag;
		}

		public int CompareTo(ShapeNode other)
		{
			if (other.List != List)
				throw new ArgumentException("Only nodes from the same list can be compared.", "other");
			return Tag.CompareTo(other.Tag);
		}

		public int CompareTo(ShapeNode other, Direction dir)
		{
			if (other.List != List)
				throw new ArgumentException("Only nodes from the same list can be compared.", "other");

			int res = Tag.CompareTo(other.Tag);
			return dir == Direction.LeftToRight ? res : -res;
		}

		public int CompareTo(object other)
		{
			if (!(other is ShapeNode))
				throw new ArgumentException("other is not an instance of a ShapeNode.", "other");
			return CompareTo((ShapeNode)other);
		}

		public ShapeNode DeepClone()
		{
			return new ShapeNode(this);
		}

		public override string ToString()
		{
			if (List != null)
			{
				if (List.Begin == this)
					return "B";
				if (List.End == this)
					return "E";
				int i = 0;
				foreach (ShapeNode node in List)
				{
					if (node == this)
						return i.ToString(CultureInfo.InvariantCulture);
					i++;
				}
			}

			return base.ToString();
		}
	}
}
