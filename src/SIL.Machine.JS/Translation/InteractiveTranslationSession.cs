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

			_wordGraphProcessor = new ErrorCorrectionWordGraphProcessor(_engine.ErrorCorrectionModel, SmtWordGraph);
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

			TranslationInfo correction = _wordGraphProcessor.Correct(Prefix, IsLastWordComplete, 1).FirstOrDefault();
			TranslationResult smtResult = CreateResult(correction);

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
			_engine.RestClient.TrainSegmentPairAsync(_engine.Id, SourceSegment, Prefix)
				.ContinueWith(t => onFinished(!t.IsFaulted));
		}

		private TranslationResult CreateResult(TranslationInfo info)
		{
			if (info == null)
			{
				return new TranslationResult(SourceSegment, Enumerable.Empty<string>(), Enumerable.Empty<double>(),
					Enumerable.Empty<TranslationSources>(), new WordAlignmentMatrix(SourceSegment.Length, 0));
			}

			double[] confidences = info.TargetConfidences.ToArray();
			var sources = new TranslationSources[info.Target.Count];
			var alignment = new WordAlignmentMatrix(SourceSegment.Length, info.Target.Count);
			int trgPhraseStartIndex = 0;
			foreach (PhraseInfo phrase in info.Phrases)
			{
				for (int j = trgPhraseStartIndex; j <= phrase.TargetCut; j++)
				{
					for (int i = phrase.SourceStartIndex; i <= phrase.SourceEndIndex; i++)
					{
						AlignmentType type = phrase.Alignment[i - phrase.SourceStartIndex, j - trgPhraseStartIndex];
						if (type == AlignmentType.Aligned)
							alignment[i, j] = AlignmentType.Aligned;
					}
					sources[j] = info.TargetUnknownWords.Contains(j) ? TranslationSources.None : TranslationSources.Smt;
				}
				trgPhraseStartIndex = phrase.TargetCut + 1;
			}

			return new TranslationResult(SourceSegment, info.Target, confidences, sources, alignment);
		}
	}
}
