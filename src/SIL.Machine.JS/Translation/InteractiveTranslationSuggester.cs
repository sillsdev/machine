using System;
using System.Linq;
using Bridge.Html5;

namespace SIL.Machine.Translation
{
	public class InteractiveTranslationSuggester
	{
		private const double RuleEngineThreshold = 0.05;

		private readonly TranslationEngine _engine;
		private readonly TranslationResult _ruleResult;
		private readonly ErrorCorrectingWordGraphProcessor _wordGraphProcessor;
		private readonly string[] _sourceSegment;

		internal InteractiveTranslationSuggester(TranslationEngine engine, WordGraph smtWordGraph, TranslationResult ruleResult, string[] sourceSegment)
		{
			_engine = engine;
			_ruleResult = ruleResult;
			_wordGraphProcessor = new ErrorCorrectingWordGraphProcessor(_engine.ErrorCorrectingModel, smtWordGraph);
			_sourceSegment = sourceSegment;
			UpdatePrefix(new string[0], true);
		}

		public string[] Prefix { get; private set; }

		public bool IsLastWordComplete { get; private set; }

		public string[] CurrentSuggestions { get; private set; }

		public string[] UpdatePrefix(string[] prefix, bool isLastWordComplete)
		{
			Prefix = prefix;
			IsLastWordComplete = isLastWordComplete;
			TranslationInfo correction = _wordGraphProcessor.Correct(prefix, isLastWordComplete, 1).FirstOrDefault();
			TranslationResult smtResult = CreateResult(correction);

			TranslationResult hybridResult = smtResult.Merge(prefix.Length, RuleEngineThreshold, _ruleResult);

			string[] suggestions = WordSuggester.GetSuggestedWordIndices(prefix, isLastWordComplete, hybridResult, _engine.ConfidenceThreshold)
				.Select(j => hybridResult.TargetSegment[j]).ToArray();

			CurrentSuggestions = suggestions;
			return CurrentSuggestions;
		}

		public void Approve(Action<bool> onFinished)
		{
			var request = new XMLHttpRequest();
			request.OnReadyStateChange = () =>
			{
				if (request.ReadyState != AjaxReadyState.Done)
					return;

				if (request.Status == 200 || request.Status == 304)
				{
					onFinished(true);
				}
				else
				{
					onFinished(false);
				}
			};

			request.Open("POST", string.Format("{0}/translation/engines/{1}/{2}/actions/train-segment", _engine.BaseUrl,
				_engine.SourceLanguageTag, _engine.TargetLanguageTag));
			request.SetRequestHeader("Content-Type", "application/json");
			request.Send(JSON.Stringify(new {sourceSegment = _sourceSegment, targetSegment = Prefix}));
		}

		private TranslationResult CreateResult(TranslationInfo info)
		{
			if (info == null)
				return new TranslationResult(_sourceSegment, Enumerable.Empty<string>(), Enumerable.Empty<double>(), new AlignedWordPair[0, 0]);

			double[] confidences = info.TargetConfidences.ToArray();
			AlignedWordPair[,] alignment = new AlignedWordPair[_sourceSegment.Length, info.Target.Count];
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

			return new TranslationResult(_sourceSegment, info.Target, confidences, alignment);
		}
	}
}
