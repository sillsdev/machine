using System;
using System.Linq;
using Bridge.Html5;

namespace SIL.Machine.Translation
{
	public class InteractiveTranslationSuggester
	{
		private const double RuleEngineThreshold = 0.05;

		private readonly TranslationEngine _engine;
		private readonly ErrorCorrectingWordGraphProcessor _wordGraphProcessor;

		internal InteractiveTranslationSuggester(TranslationEngine engine, WordGraph smtWordGraph, TranslationResult ruleResult, string[] sourceSegment)
		{
			_engine = engine;
			RuleResult = ruleResult;
			SmtWordGraph = smtWordGraph;
			SourceSegment = sourceSegment;

			_wordGraphProcessor = new ErrorCorrectingWordGraphProcessor(_engine.ErrorCorrectingModel, SmtWordGraph);
			UpdatePrefix(new string[0], true);
		}

		public WordGraph SmtWordGraph { get; }
		public TranslationResult RuleResult { get; }

		public string[] SourceSegment { get; }

		public string[] Prefix { get; private set; }

		public bool IsLastWordComplete { get; private set; }

		public string[] CurrentSuggestions { get; private set; }

		public string[] UpdatePrefix(string[] prefix, bool isLastWordComplete)
		{
			Prefix = prefix;
			IsLastWordComplete = isLastWordComplete;
			TranslationInfo correction = _wordGraphProcessor.Correct(prefix, isLastWordComplete, 1).FirstOrDefault();
			TranslationResult smtResult = CreateResult(correction);

			TranslationResult hybridResult = smtResult.Merge(prefix.Length, RuleEngineThreshold, RuleResult);

			string[] suggestions = WordSuggester.GetSuggestedWordIndices(prefix, isLastWordComplete, hybridResult, _engine.ConfidenceThreshold)
				.Select(j => hybridResult.TargetSegment[j]).ToArray();

			CurrentSuggestions = suggestions;
			return CurrentSuggestions;
		}

		public void Approve(Action<bool> onFinished)
		{
			string url = string.Format("{0}/translation/engines/{1}/{2}/actions/train-segment", _engine.BaseUrl, _engine.SourceLanguageTag, _engine.TargetLanguageTag);
			string body = JSON.Stringify(new {sourceSegment = SourceSegment, targetSegment = Prefix});
			_engine.WebClient.Send("POST", url, body, "application/json", responseText => onFinished(true), status => onFinished(false));
		}

		private TranslationResult CreateResult(TranslationInfo info)
		{
			if (info == null)
				return new TranslationResult(SourceSegment, Enumerable.Empty<string>(), Enumerable.Empty<double>(), new AlignedWordPair[0, 0]);

			double[] confidences = info.TargetConfidences.ToArray();
			AlignedWordPair[,] alignment = new AlignedWordPair[SourceSegment.Length, info.Target.Count];
			int trgPhraseStartIndex = 0;
			foreach (PhraseInfo phrase in info.Phrases)
			{
				for (int j = trgPhraseStartIndex; j <= phrase.TargetCut; j++)
				{
					for (int i = phrase.SourceStartIndex; i <= phrase.SourceEndIndex; i++)
					{
						if (phrase.Alignment[i - phrase.SourceStartIndex, j - trgPhraseStartIndex] == AlignmentType.Aligned)
						{
							TranslationSources sources = TranslationSources.None;
							if (!info.TargetUnknownWords.Contains(j))
								sources = TranslationSources.Smt;
							alignment[i, j] = new AlignedWordPair(i, j, sources);
						}
					}
				}
				trgPhraseStartIndex = phrase.TargetCut + 1;
			}

			return new TranslationResult(SourceSegment, info.Target, confidences, alignment);
		}
	}
}
