namespace SIL.Machine.Translation
{
	public interface ITranslationSuggester
	{
		double ConfidenceThreshold { get; set; }

		TranslationSuggestion GetSuggestion(int prefixCount, bool isLastWordComplete, TranslationResult result);
	}
}
