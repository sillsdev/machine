using System;
using SIL.APRE;

namespace SIL.HermitCrab
{
	public class PhoneticShapeNode : BidirListNode<PhoneticShapeNode>, IComparable<PhoneticShapeNode>, IComparable
	{
		private readonly Annotation<PhoneticShapeNode> _ann;

		public PhoneticShapeNode(SpanFactory<PhoneticShapeNode> spanFactory, string type, FeatureStructure fs)
		{
			_ann = new Annotation<PhoneticShapeNode>(type, spanFactory.Create(this), fs);
			Tag = int.MinValue;
		}

		public int Tag { get; internal set; }

		public Annotation<PhoneticShapeNode> Annotation
		{
			get { return _ann; }
		}

		public int CompareTo(PhoneticShapeNode other)
		{
			if (other.List != List)
				throw new ArgumentException("Only atoms from the same list can be compared.", "other");
			return Tag.CompareTo(other.Tag);
		}

		public int CompareTo(PhoneticShapeNode other, Direction dir)
		{
			if (other.List != List)
				throw new ArgumentException("Only atoms from the same list can be compared.", "other");

			int res = Tag.CompareTo(other.Tag);
			return dir == Direction.LeftToRight ? res : -res;
		}

		public int CompareTo(object other)
		{
			if (!(other is PhoneticShapeNode))
				throw new ArgumentException("other is not an instance of an Atom object.", "other");
			return CompareTo((PhoneticShapeNode)other);
		}
	}
}
