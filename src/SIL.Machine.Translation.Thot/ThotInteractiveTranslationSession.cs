using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;
using System;

namespace SIL.Machine.Translation.Thot
{
	internal class ThotInteractiveTranslationSession : DisposableBase, IInteractiveTranslationSession
	{
		private readonly ThotSmtEngine _engine;
		private readonly IReadOnlyList<string> _sourceSegment;
		private readonly int _n;
		private List<string> _prefix;
		private bool _isLastWordComplete;
		private TranslationResult[] _currentResults;
		private readonly ErrorCorrectionWordGraphProcessor _wordGraphProcessor;

		public ThotInteractiveTranslationSession(ThotSmtEngine engine, int n, IReadOnlyList<string> sourceSegment,
			WordGraph wordGraph)
		{
			_engine = engine;
			_sourceSegment = sourceSegment;
			_n = n;
			_prefix = new List<string>();
			_isLastWordComplete = true;
			_wordGraphProcessor = new ErrorCorrectionWordGraphProcessor(_engine.ErrorCorrectionModel, _sourceSegment,
				wordGraph);
			UpdateInteractiveResults();
		}

		public IReadOnlyList<string> SourceSegment
		{
			get
			{
				CheckDisposed();
				return _sourceSegment;
			}
		}

		public IReadOnlyList<string> Prefix
		{
			get
			{
				CheckDisposed();
				return _prefix;
			}
		}

		public bool IsLastWordComplete
		{
			get
			{
				CheckDisposed();
				return _isLastWordComplete;
			}
		}

		public IReadOnlyList<TranslationResult> CurrentResults
		{
			get
			{
				CheckDisposed();
				return _currentResults;
			}
		}

		private void UpdateInteractiveResults()
		{
			_currentResults = _wordGraphProcessor.Correct(_prefix.ToArray(), _isLastWordComplete, _n).ToArray();
		}

		public IReadOnlyList<TranslationResult> SetPrefix(IReadOnlyList<string> prefix, bool isLastWordComplete)
		{
			CheckDisposed();

			if (!_prefix.SequenceEqual(prefix) || _isLastWordComplete != isLastWordComplete)
			{
				_prefix.Clear();
				_prefix.AddRange(prefix);
				_isLastWordComplete = isLastWordComplete;
				UpdateInteractiveResults();
			}
			return _currentResults;
		}

		public IReadOnlyList<TranslationResult> AppendToPrefix(string addition, bool isLastWordComplete)
		{
			CheckDisposed();

			if (string.IsNullOrEmpty(addition) && _isLastWordComplete)
			{
				throw new ArgumentException(
					"An empty string cannot be added to a prefix where the last word is complete.", nameof(addition));
			}

			if (!string.IsNullOrEmpty(addition) || isLastWordComplete != _isLastWordComplete)
			{
				if (_isLastWordComplete)
					_prefix.Add(addition);
				else
					_prefix[_prefix.Count - 1] = _prefix[_prefix.Count - 1] + addition;
				_isLastWordComplete = isLastWordComplete;
				UpdateInteractiveResults();
			}
			return _currentResults;
		}

		public IReadOnlyList<TranslationResult> AppendToPrefix(IEnumerable<string> words)
		{
			CheckDisposed();

			bool updated = false;
			foreach (string word in words)
			{
				if (_isLastWordComplete)
					_prefix.Add(word);
				else
					_prefix[_prefix.Count - 1] = word;
				_isLastWordComplete = true;
				updated = true;
			}
			if (updated)
				UpdateInteractiveResults();
			return _currentResults;
		}

		public void Approve(bool alignedOnly)
		{
			CheckDisposed();

			IReadOnlyList<string> sourceSegment = _sourceSegment;
			if (alignedOnly)
			{
				if (_currentResults.Length == 0)
					return;
				sourceSegment = _currentResults[0].GetAlignedSourceSegment(_prefix.Count);
			}

			if (sourceSegment.Count > 0)
				_engine.TrainSegment(sourceSegment, _prefix);
		}

		protected override void DisposeManagedResources()
		{
			_engine.RemoveSession(this);
		}
	}
}
