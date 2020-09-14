﻿using System.Collections.Generic;
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
			IReadOnlyList<string> sourceSegment = SourceSegment;
			if (alignedOnly)
			{
				TranslationResult bestResult = GetCurrentResults().FirstOrDefault();
				if (bestResult == null)
					return;
				sourceSegment = bestResult.GetAlignedSourceSegment(_prefix.Count);
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
	}
}