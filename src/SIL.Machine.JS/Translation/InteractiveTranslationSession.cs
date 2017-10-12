using System;
using System.Linq;
using SIL.Machine.Annotations;

namespace SIL.Machine.Translation
{
	public class InteractiveTranslationSession
	{
		private const double RuleEngineThreshold = 0.05;

		private readonly TranslationEngine _engine;
		private readonly ErrorCorrectionWordGraphProcessor _wordGraphProcessor;
		private TranslationResult _curResult;
		private double _confidenceThreshold;

		internal InteractiveTranslationSession(TranslationEngine engine, string[] sourceSegment,
			double confidenceThreshold, InteractiveTranslationResult result)
		{
			_engine = engine;
			SourceSegment = sourceSegment;
			_confidenceThreshold = confidenceThreshold;
			RuleResult = result.RuleResult;
			SmtWordGraph = result.SmtWordGraph;

			_wordGraphProcessor = new ErrorCorrectionWordGraphProcessor(_engine.ErrorCorrectionModel, SourceSegment,
				SmtWordGraph);
			UpdatePrefix("");
		}

		internal WordGraph SmtWordGraph { get; }
		internal TranslationResult RuleResult { get; }

		public string[] SourceSegment { get; }

		public double ConfidenceThreshold
		{
			get { return _confidenceThreshold; }
			set
			{
				if (_confidenceThreshold != value)
				{
					_confidenceThreshold = value;
					UpdateSuggestion();
				}
			}
		}

		public string[] Prefix { get; private set; }

		public bool IsLastWordComplete { get; private set; }

		public string[] CurrentSuggestion { get; private set; }

		public string[] UpdatePrefix(string prefix)
		{
			Range<int>[] tokenRanges = _engine.TargetWordTokenizer.Tokenize(prefix).ToArray();
			Prefix = tokenRanges.Select(s => prefix.Substring(s.Start, s.Length)).ToArray();
			IsLastWordComplete = tokenRanges.Length == 0 || tokenRanges[tokenRanges.Length - 1].End != prefix.Length;

			TranslationResult smtResult = _wordGraphProcessor.Correct(Prefix, IsLastWordComplete, 1).First();

			if (RuleResult == null)
			{
				_curResult = smtResult;
			}
			else
			{
				int prefixCount = Prefix.Length;
				if (!IsLastWordComplete)
					prefixCount--;

				_curResult = smtResult.Merge(prefixCount, RuleEngineThreshold, RuleResult);
			}

			UpdateSuggestion();

			return CurrentSuggestion;
		}

		public TextInsertion GetSuggestionTextInsertion(int suggestionIndex = -1)
		{
			string text = suggestionIndex == -1 ? string.Join(" ", CurrentSuggestion)
				: CurrentSuggestion[suggestionIndex];
			if (IsLastWordComplete)
				return new TextInsertion { InsertText = text };


			string lastToken = Prefix[Prefix.Length - 1];
			if (suggestionIndex > 0)
				return new TextInsertion { DeleteLength = lastToken.Length, InsertText = text };

			return new TextInsertion { InsertText = text.Substring(lastToken.Length, text.Length - lastToken.Length) };
		}

		private void UpdateSuggestion()
		{
			string[] suggestions = TranslationSuggester.GetSuggestedWordIndices(Prefix, IsLastWordComplete, _curResult,
				_confidenceThreshold).Select(j => _curResult.TargetSegment[j]).ToArray();

			CurrentSuggestion = suggestions;
		}

		public void Approve(Action<bool> onFinished)
		{
			_engine.RestClient.TrainSegmentPairAsync(_engine.ProjectId, SourceSegment, Prefix)
				.ContinueWith(t => onFinished(!t.IsFaulted));
		}
	}
}
