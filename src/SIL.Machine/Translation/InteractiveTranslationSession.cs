using System.Collections.Generic;
using System.Linq;
using System;

namespace SIL.Machine.Translation
{
	public class InteractiveTranslationSession
	{
		private readonly InteractiveTranslator _interactiveTranslator;
		private readonly int _n;
		private List<string> _prefix;
		private readonly ErrorCorrectionWordGraphProcessor _wordGraphProcessor;

		public InteractiveTranslationSession(InteractiveTranslator interactiveTranslator, int n,
			IReadOnlyList<string> sourceSegment, WordGraph wordGraph)
		{
			_interactiveTranslator = interactiveTranslator;
			SourceSegment = sourceSegment;
			_n = n;
			_prefix = new List<string>();
			IsLastWordComplete = true;
			_wordGraphProcessor = new ErrorCorrectionWordGraphProcessor(_interactiveTranslator.ErrorCorrectionModel,
				SourceSegment, wordGraph);
			UpdateInteractiveResults();
		}

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> Prefix => _prefix;

		public bool IsLastWordComplete { get; private set; }

		public IReadOnlyList<TranslationResult> CurrentResults { get; private set; }

		private void UpdateInteractiveResults()
		{
			CurrentResults = _wordGraphProcessor.Correct(_prefix.ToArray(), IsLastWordComplete, _n).ToArray();
		}

		public IReadOnlyList<TranslationResult> SetPrefix(IReadOnlyList<string> prefix, bool isLastWordComplete)
		{
			if (!_prefix.SequenceEqual(prefix) || IsLastWordComplete != isLastWordComplete)
			{
				_prefix.Clear();
				_prefix.AddRange(prefix);
				IsLastWordComplete = isLastWordComplete;
				UpdateInteractiveResults();
			}
			return CurrentResults;
		}

		public IReadOnlyList<TranslationResult> AppendToPrefix(string addition, bool isLastWordComplete)
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
				UpdateInteractiveResults();
			}
			return CurrentResults;
		}

		public IReadOnlyList<TranslationResult> AppendToPrefix(IEnumerable<string> words)
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
				UpdateInteractiveResults();
			return CurrentResults;
		}

		public IReadOnlyList<TranslationResult> AppendSuggestionToPrefix(int resultIndex, IReadOnlyList<int> suggestion)
		{
			return AppendToPrefix(suggestion.Select(j => CurrentResults[resultIndex].TargetSegment[j]));
		}

		public void Approve(bool alignedOnly)
		{
			IReadOnlyList<string> sourceSegment = SourceSegment;
			if (alignedOnly)
			{
				if (CurrentResults.Count == 0)
					return;
				sourceSegment = CurrentResults[0].GetAlignedSourceSegment(_prefix.Count);
			}

			if (sourceSegment.Count > 0)
				_interactiveTranslator.Engine.TrainSegment(sourceSegment, _prefix);
		}
	}
}
