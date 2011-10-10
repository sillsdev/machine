namespace SIL.APRE.Matching
{
	public enum AnchorType
	{
		LeftSide,
		RightSide
	}

	public class Anchor<TOffset> : PatternNode<TOffset>
	{
		private readonly AnchorType _type;

		public Anchor(AnchorType type)
		{
			_type = type;
		}

		public Anchor(Anchor<TOffset> anchor)
		{
			_type = anchor._type;
		}

		public AnchorType Type
		{
			get { return _type; }
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Anchor<TOffset>(this);
		}

		public override string ToString()
		{
			return _type == AnchorType.LeftSide ? "^" : "$";
		}
	}
}
