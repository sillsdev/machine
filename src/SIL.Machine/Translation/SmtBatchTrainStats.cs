namespace SIL.Machine.Translation
{
	public class SmtBatchTrainStats
	{
		public int TrainedSegmentCount { get; set; }
		public double TranslationModelBleu { get; set; }
		public double LanguageModelPerplexity { get; set; }
	}
}
