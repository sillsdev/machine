namespace SIL.Machine.Translation
{
	public class HybridInteractiveTranslationResult
	{
		public HybridInteractiveTranslationResult(WordGraph smtWordGraph, TranslationResult ruleResult)
		{
			SmtWordGraph = smtWordGraph;
			RuleResult = ruleResult;
		}

		public WordGraph SmtWordGraph { get; }
		public TranslationResult RuleResult { get; }
	}
}
