using System;
using SIL.APRE;
using SIL.APRE.FeatureModel;

namespace SIL.HermitCrab
{
	public class ShapeNode : BidirListNode<ShapeNode>, IComparable<ShapeNode>, IComparable
	{
		private readonly Annotation<ShapeNode> _ann;

		public ShapeNode(string type, SpanFactory<ShapeNode> spanFactory, FeatureStruct fs)
		{
			_ann = new Annotation<ShapeNode>(type, spanFactory.Create(this), fs);
			Tag = int.MinValue;
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
