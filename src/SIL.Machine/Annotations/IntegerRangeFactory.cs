using SIL.Machine.DataStructures;

namespace SIL.Machine.Annotations
{
	internal class IntegerRangeFactory : RangeFactory<int>
	{
		public IntegerRangeFactory()
		{
			Null = new Range<int>(-1, -1);
		}

		public override int GetLength(int start, int end)
		{
			return end - start;
		}

		public override Range<int> Create(int offset, Direction dir)
		{
			return Create(offset, offset + (dir == Direction.LeftToRight ? 1 : -1), dir);
		}
	}
}
