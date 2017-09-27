using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	internal class ThotInteractiveTranslationSession : DisposableBase, IInteractiveTranslationSession
	{
		private readonly ThotSmtEngine _engine;
		private readonly IReadOnlyList<string> _sourceSegment; 
		private string[] _prefix;
		private bool _isLastWordComplete;
		private TranslationResult _currentResult;
		private readonly ErrorCorrectionWordGraphProcessor _wordGraphProcessor;

		public ThotInteractiveTranslationSession(ThotSmtEngine engine, IReadOnlyList<string> sourceSegment, WordGraph wordGraph)
		{
			_engine = engine;
			_sourceSegment = sourceSegment;
			_prefix = new string[0];
			_isLastWordComplete = true;
			_wordGraphProcessor = new ErrorCorrectionWordGraphProcessor(_engine.ErrorCorrectionModel, wordGraph);
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

		public TranslationResult CurrentResult
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
			return _engine.CreateResult(_sourceSegment, _prefix.Length, correction);
		}

		public TranslationResult SetPrefix(IReadOnlyList<string> prefix, bool isLastWordComplete)
		{
			CheckDisposed();

			if (!_prefix.SequenceEqual(prefix) || _isLastWordComplete != isLastWordComplete)
			{
				_prefix = prefix.ToArray();
				_isLastWordComplete = isLastWordComplete;
				_currentResult = CreateInteractiveResult();
			}
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
