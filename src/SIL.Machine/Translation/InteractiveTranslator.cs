using System.Collections.Generic;
using System.Linq;
using System;

namespace SIL.Machine.Translation
{
	public class InteractiveTranslator
	{
		public static InteractiveTranslator Create(ErrorCorrectionModel ecm, IInteractiveTranslationEngine engine,
			IReadOnlyList<string> segment)
		{
			return new InteractiveTranslator(ecm, engine, segment, engine.GetWordGraph(segment));
		}

		private readonly IInteractiveTranslationEngine _engine;
		private List<string> _prefix;
		private readonly ErrorCorrectionWordGraphProcessor _wordGraphProcessor;

		private InteractiveTranslator(ErrorCorrectionModel ecm, IInteractiveTranslationEngine engine,
			IReadOnlyList<string> sourceSegment, WordGraph wordGraph)
		{
			_engine = engine;
			SourceSegment = sourceSegment;
			_prefix = new List<string>();
			IsLastWordComplete = true;
			_wordGraphProcessor = new ErrorCorrectionWordGraphProcessor(ecm, SourceSegment, wordGraph);
			Correct();
		}

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> Prefix => _prefix;

		public bool IsLastWordComplete { get; private set; }

		public bool IsSourceSegmentValid => SourceSegment.Count <= TranslationConstants.MaxSegmentLength;

		public void SetPrefix(IReadOnlyList<string> prefix, bool isLastWordComplete)
		{
			if (!_prefix.SequenceEqual(prefix) || IsLastWordComplete != isLastWordComplete)
			{
				_prefix.Clear();
				_prefix.AddRange(prefix);
				IsLastWordComplete = isLastWordComplete;
				Correct();
			}
		}

		public void AppendToPrefix(string addition, bool isLastWordComplete)
		{
			if (string.IsNullOrEmpty(addition) && IsLastWordComplete)
			{
				throw new ArgumentException(
					"An empty string cannot be added to a prefix where the last word is complete.", nameof(addition));
			}

			if (!string.IsNullOrEmpty(addition) || isLastWordComplete != IsLastWordComplete)
			{
				if (IsLastWordComplete)
					_prefix.Add(addition);
				else
					_prefix[_prefix.Count - 1] = _prefix[_prefix.Count - 1] + addition;
				IsLastWordComplete = isLastWordComplete;
				Correct();
			}
		}

		public void AppendToPrefix(params string[] words)
		{
			AppendToPrefix((IEnumerable<string>)words);
		}

		public void AppendToPrefix(IEnumerable<string> words)
		{
			bool updated = false;
			foreach (string word in words)
			{
				if (IsLastWordComplete)
					_prefix.Add(word);
				else
					_prefix[_prefix.Count - 1] = word;
				IsLastWordComplete = true;
				updated = true;
			}
			if (updated)
				Correct();
		}

		public void Approve(bool alignedOnly)
		{
			if (!IsSourceSegmentValid || _prefix.Count > TranslationConstants.MaxSegmentLength)
				return;

			IReadOnlyList<string> sourceSegment = SourceSegment;
			if (alignedOnly)
			{
				TranslationResult bestResult = GetCurrentResults().FirstOrDefault();
				if (bestResult == null)
					return;
				sourceSegment = GetAlignedSourceSegment(bestResult);
			}

			if (sourceSegment.Count > 0)
				_engine.TrainSegment(sourceSegment, _prefix);
		}

		public IEnumerable<TranslationResult> GetCurrentResults()
		{
			return _wordGraphProcessor.GetResults();
		}

		private void Correct()
		{
			_wordGraphProcessor.Correct(_prefix.ToArray(), IsLastWordComplete);
		}

		private IReadOnlyList<string> GetAlignedSourceSegment(TranslationResult result)
		{
			int sourceLength = 0;
			foreach (Phrase phrase in result.Phrases)
			{
				if (phrase.TargetSegmentCut > _prefix.Count)
					break;

				if (phrase.SourceSegmentRange.End > sourceLength)
					sourceLength = phrase.SourceSegmentRange.End;
			}

			return sourceLength == SourceSegment.Count ? SourceSegment : SourceSegment.Take(sourceLength).ToArray();
		}
	}
}
