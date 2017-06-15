namespace SIL.Machine.Translation
{
	public class InteractiveTranslationResult
	{
		public InteractiveTranslationResult(WordGraph smtWordGraph, TranslationResult ruleResult)
		{
			SmtWordGraph = smtWordGraph;
			RuleResult = ruleResult;
		}

		public WordGraph SmtWordGraph { get; }
		public TranslationResult RuleResult { get; }
	}
}
