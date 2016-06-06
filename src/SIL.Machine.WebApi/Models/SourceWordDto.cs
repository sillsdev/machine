namespace SIL.Machine.WebApi.Models
{
	public class SourceWordDto
	{
		public SourceWordDto(RangeDto range, double confidence)
		{
			Range = range;
			Confidence = confidence;
		}

		public RangeDto Range { get; }
		public double Confidence { get; }
	}
}
