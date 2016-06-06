namespace SIL.Machine.WebApi.Models
{
	public class RangeDto
	{
		public RangeDto(int startIndex, int endIndex)
		{
			StartIndex = startIndex;
			EndIndex = endIndex;
		}

		public int StartIndex { get; }
		public int EndIndex { get; }
	}
}
