using System;
using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	internal class ThotInteractiveTranslationSession : DisposableBase, IInteractiveTranslationSession
	{
		private readonly ThotSmtEngine _engine;
		private readonly IReadOnlyList<string> _sourceSegment; 
		private readonly List<string> _prefix;
		private bool _isLastWordComplete;
		private TranslationResult _currentResult;
		private readonly ErrorCorrectingWordGraphProcessor _wordGraphProcessor;

		internal ThotInteractiveTranslationSession(ThotSmtEngine engine, IReadOnlyList<string> sourceSegment, WordGraph wordGraph)
		{
			_engine = engine;
			_sourceSegment = sourceSegment;
			_prefix = new List<string>();
			_isLastWordComplete = true;
			_wordGraphProcessor = new ErrorCorrectingWordGraphProcessor(_engine.ErrorCorrectingModel, wordGraph);
			_currentResult = CreateInteractiveResult();
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

		public TranslationResult CurrenTranslationResult
		{
			get
			{
				CheckDisposed();
				return _currentResult;
			}
		}

		private TranslationResult CreateInteractiveResult()
		{
			TranslationInfo correction = _wordGraphProcessor.Correct(_prefix, _isLastWordComplete, 1).FirstOrDefault();
			return _engine.CreateResult(_sourceSegment, correction);
		}

		public TranslationResult AddToPrefix(IEnumerable<string> addition, bool isLastWordComplete)
		{
			CheckDisposed();
			if (_wordGraphProcessor == null)
				throw new InvalidOperationException("An interactive translation has not been started.");

			_prefix.AddRange(addition);
			_isLastWordComplete = isLastWordComplete;
			_currentResult = CreateInteractiveResult();
			return _currentResult;
		}

		public TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordComplete)
		{
			CheckDisposed();
			if (_wordGraphProcessor == null)
				throw new InvalidOperationException("An interactive translation has not been started.");

			_prefix.Clear();
			_prefix.AddRange(prefix);
			_isLastWordComplete = isLastWordComplete;
			_currentResult = CreateInteractiveResult();
			return _currentResult;
		}

		public void Approve()
		{
			CheckDisposed();

			_engine.TrainSegment(_sourceSegment, _prefix);
		}

		protected override void DisposeManagedResources()
		{
			_engine.RemoveSession(this);
		}
	}
}
