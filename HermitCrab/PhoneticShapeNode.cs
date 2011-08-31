using System;
using SIL.APRE;
using SIL.APRE.FeatureModel;

namespace SIL.HermitCrab
{
	public class PhoneticShapeNode : BidirListNode<PhoneticShapeNode>, IComparable<PhoneticShapeNode>, IComparable, ICloneable
	{
		private readonly Annotation<PhoneticShapeNode> _ann;

		public PhoneticShapeNode(SpanFactory<PhoneticShapeNode> spanFactory, FeatureStruct fs)
		{
			_ann = new Annotation<PhoneticShapeNode>(spanFactory.Create(this), fs);
			Tag = int.MinValue;
		}

		public PhoneticShapeNode(PhoneticShapeNode node)
		{
			_ann = node._ann.Clone();
		}

		protected override void Init(BidirList<PhoneticShapeNode> list)
		{
			base.Init(list);
			((PhoneticShape) List).Annotations.Add(_ann);
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

		public PhoneticShapeNode Clone()
		{
			return new PhoneticShapeNode(this);
		}

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
