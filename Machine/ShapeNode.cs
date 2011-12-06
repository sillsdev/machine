using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine
{
	public class ShapeNode : BidirListNode<ShapeNode>, IComparable<ShapeNode>, IComparable, ICloneable<ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory; 
		private readonly Annotation<ShapeNode> _ann;

		public ShapeNode(SpanFactory<ShapeNode> spanFactory, string type, FeatureStruct fs)
		{
			_spanFactory = spanFactory;
			_ann = new Annotation<ShapeNode>(type, spanFactory.Create(this), fs);
			Tag = int.MinValue;
		}

		public ShapeNode(ShapeNode node)
			: this(node._spanFactory, node.Annotation.Type, node.Annotation.FeatureStruct.Clone())
		{
		}

		public int Tag { get; internal set; }

		public Annotation<ShapeNode> Annotation
		{
			get { return _ann; }
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

		public ShapeNode Clone()
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
						return i.ToString();
					i++;
				}
			}

			return base.ToString();
		}
	}
}
