using System;
using System.Linq;
using SIL.Machine.Annotations;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public class InteractiveTranslationSession
	{
		private const double RuleEngineThreshold = 0.05;

		private readonly TranslationEngine _engine;
		private ErrorCorrectionWordGraphProcessor _wordGraphProcessor;
		private readonly ITranslationSuggester _suggester;
		private TranslationResult _curResult;

		internal InteractiveTranslationSession(TranslationEngine engine, string[] sourceSegment,
			double confidenceThreshold, InteractiveTranslationResult result)
		{
			_engine = engine;
			_suggester = new PhraseTranslationSuggester { ConfidenceThreshold = confidenceThreshold };
			SourceSegment = sourceSegment;
			RuleResult = result.RuleResult;
			SmtWordGraph = result.SmtWordGraph;
		}

		internal WordGraph SmtWordGraph { get; }
		internal TranslationResult RuleResult { get; }

		public string[] SourceSegment { get; }

		public double ConfidenceThreshold
		{
			get { return _suggester.ConfidenceThreshold; }
			set
			{
				if (_suggester.ConfidenceThreshold != value)
				{
					_suggester.ConfidenceThreshold = value;
					UpdateSuggestion();
				}
			}
		}

		public string[] Prefix { get; private set; }

		public bool IsLastWordComplete { get; private set; }

		public string[] Suggestion { get; private set; }
		public double SuggestionConfidence { get; private set; }

		public bool IsInitialized => _wordGraphProcessor != null;

		public bool IsSourceSegmentValid => SourceSegment.Length <= TranslationEngine.MaxSegmentSize;

		public void Initialize()
		{
			if (IsInitialized)
				return;

			_wordGraphProcessor = new ErrorCorrectionWordGraphProcessor(_engine.ErrorCorrectionModel, SourceSegment,
				SmtWordGraph);
			UpdatePrefix("");
		}

		public string[] UpdatePrefix(string prefix)
		{
			if (!IsInitialized)
				throw new InvalidOperationException("The session has not been initialized.");

			Range<int>[] tokenRanges = _engine.TargetWordTokenizer.Tokenize(prefix).ToArray();
			Prefix = tokenRanges.Select(s => prefix.Substring(s.Start, s.Length)).ToArray();
			IsLastWordComplete = tokenRanges.Length == 0 || tokenRanges[tokenRanges.Length - 1].End != prefix.Length;

			TranslationResult smtResult = _wordGraphProcessor.Correct(Prefix, IsLastWordComplete, 1).FirstOrDefault();
			if (smtResult == null)
			{
				var builder = new TranslationResultBuilder();
				smtResult = builder.ToResult(SourceSegment, Prefix.Length);
			}

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

			return Suggestion;
		}

		public string GetSuggestionText(int suggestionIndex = -1)
		{
			if (!IsInitialized)
				throw new InvalidOperationException("The session has not been initialized.");

			IEnumerable<string> words = suggestionIndex == -1 ? (IEnumerable<string>) Suggestion
				: Suggestion.Take(suggestionIndex + 1);
			// TODO: use detokenizer to build suggestion text
			string text = string.Join(" ", words);

			if (IsLastWordComplete)
				return text;

			string lastToken = Prefix[Prefix.Length - 1];
			return text.Substring(lastToken.Length, text.Length - lastToken.Length);
		}

		private void UpdateSuggestion()
		{
			TranslationSuggestion suggestion = _suggester.GetSuggestion(Prefix.Length, IsLastWordComplete, _curResult);
			Suggestion = suggestion.TargetWordIndices.Select(j => _curResult.TargetSegment[j]).ToArray();
			SuggestionConfidence = suggestion.Confidence;
		}

		public void Approve(Action<bool> onFinished)
		{
			if (!IsInitialized)
				throw new InvalidOperationException("The session has not been initialized.");

			_engine.RestClient.TrainSegmentPairAsync(_engine.ProjectId, SourceSegment, Prefix)
				.ContinueWith(t => onFinished(!t.IsFaulted));
		}
	}
}
