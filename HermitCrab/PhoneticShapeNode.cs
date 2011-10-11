using System;
using SIL.APRE;
using SIL.APRE.FeatureModel;

namespace SIL.HermitCrab
{
	public class PhoneticShapeNode : BidirListNode<PhoneticShapeNode>, IComparable<PhoneticShapeNode>, IComparable, ICloneable
	{
		private readonly Annotation<PhoneticShapeNode> _ann;

		public PhoneticShapeNode(string type, SpanFactory<PhoneticShapeNode> spanFactory, FeatureStruct fs)
		{
			_ann = new Annotation<PhoneticShapeNode>(type, spanFactory.Create(this), fs);
			Tag = int.MinValue;
		}

		public PhoneticShapeNode(PhoneticShapeNode node)
		{
			_ann = node._ann.Clone();
		}

		public int Tag { get; internal set; }

		public Annotation<PhoneticShapeNode> Annotation
		{
			get { return _ann; }
		}

		public int CompareTo(PhoneticShapeNode other)
		{
			if (other.List != List)
				throw new ArgumentException("Only nodes from the same list can be compared.", "other");
			return Tag.CompareTo(other.Tag);
		}

		public int CompareTo(PhoneticShapeNode other, Direction dir)
		{
			if (other.List != List)
				throw new ArgumentException("Only nodes from the same list can be compared.", "other");

			int res = Tag.CompareTo(other.Tag);
			return dir == Direction.LeftToRight ? res : -res;
		}

		public int CompareTo(object other)
		{
			if (!(other is PhoneticShapeNode))
				throw new ArgumentException("other is not an instance of a PhoneticShapeNode.", "other");
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

		public override string ToString()
		{
			if (List != null)
			{
				if (List.Begin == this)
					return "B";
				if (List.End == this)
					return "E";
				int i = 0;
				foreach (PhoneticShapeNode node in List)
				{
					if (node == this)
						return i.ToString();
					i++;
				}
			}

			return base.ToString();

			//var shape = (PhoneticShape) List;

			//var sb = new StringBuilder();
			//SegmentDefinition[] segDefs = shape.CharacterDefinitionTable.GetMatchingSegmentDefinitions(this, shape.Mode).ToArray();
			//if (segDefs.Length > 1)
			//    sb.Append("[");
			//foreach (SegmentDefinition segDef in segDefs)
			//{
			//    if (segDef.StrRep.Length > 1)
			//        sb.Append("(");
			//    sb.Append(segDef.StrRep);
			//    if (segDef.StrRep.Length > 1)
			//        sb.Append(")");
			//}
			//if (segDefs.Length > 1)
			//    sb.Append("]");
			//return sb.ToString();
		}
	}
}
