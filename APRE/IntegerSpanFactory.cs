namespace SIL.APRE
{
	public class IntegerSpanFactory : SpanFactory<int>
	{
		public IntegerSpanFactory()
			: base(Compare, CalcLength, false)
		{
		}

		private static int Compare(int x, int y)
		{
			return x.CompareTo(y);
		}

		private static int CalcLength(int start, int end)
		{
			return end - start;
		}
	}
}
