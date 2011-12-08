namespace SIL.Machine
{
	public class IntegerSpanFactory : SpanFactory<int>
	{
		private readonly Span<int> _empty; 

		public IntegerSpanFactory()
			: base(false)
		{
			_empty = new Span<int>(this, -1, -1);
		}

		public override Span<int> Empty
		{
			get { return _empty; }
		}

		public override int Compare(int x, int y)
		{
			return x.CompareTo(y);
		}

		public override int CalcLength(int start, int end)
		{
			return end - start;
		}
	}
}
