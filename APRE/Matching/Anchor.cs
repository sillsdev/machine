using System;

namespace SIL.APRE.Matching
{
	[Flags]
	public enum AnchorTypes
	{
		None = 0x0,
		LeftSide = 0x1,
		RightSide = 0x2
	}

	public class Anchor<TOffset> : PatternNode<TOffset>
	{
		private readonly AnchorTypes _type;

		public Anchor(AnchorTypes type)
		{
			_type = type;
		}

		public Anchor(Anchor<TOffset> anchor)
		{
			_type = anchor._type;
		}

		public AnchorTypes Type
		{
			get { return _type; }
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Anchor<TOffset>(this);
		}

		public override string ToString()
		{
			return _type == AnchorTypes.LeftSide ? "^" : "$";
		}
	}
}
