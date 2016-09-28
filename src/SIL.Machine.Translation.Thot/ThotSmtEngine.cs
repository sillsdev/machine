using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.ObjectModel;
using SIL.Progress;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSmtEngine : DisposableBase, IInteractiveSmtEngine
	{
		public static void TrainModels(string cfgFileName, Func<string, string> sourcePreprocessor, ITokenizer<string, int> sourceTokenizer, ITextCorpus sourceCorpus,
			Func<string, string> targetPreprocessor, ITokenizer<string, int> targetTokenizer, ITextCorpus targetCorpus, IProgress progress = null)
		{
			var trainer = new ThotBatchTrainer(cfgFileName, sourcePreprocessor, sourceTokenizer, sourceCorpus, targetPreprocessor, targetTokenizer, targetCorpus);
			trainer.Train(progress);
		}

		private readonly string _cfgFileName;
		private IntPtr _handle;
		private readonly HashSet<ThotSmtSession> _sessions;
		private ThotSmtSession _globalSession;
		private readonly ThotSingleWordAlignmentModel _singleWordAlignmentModel;
		private readonly ThotSingleWordAlignmentModel _inverseSingleWordAlignmentModel;

		public ThotSmtEngine(string cfgFileName)
		{
			if (!File.Exists(cfgFileName))
				throw new FileNotFoundException("The Thot configuration file could not be found.", cfgFileName);
			_cfgFileName = cfgFileName;
			_sessions = new HashSet<ThotSmtSession>();
			_handle = Thot.decoder_open(_cfgFileName);
			_singleWordAlignmentModel = new ThotSingleWordAlignmentModel(Thot.decoder_getSingleWordAlignmentModel(_handle));
			_inverseSingleWordAlignmentModel = new ThotSingleWordAlignmentModel(Thot.decoder_getInverseSingleWordAlignmentModel(_handle));
		}

		public void Train(Func<string, string> sourcePreprocessor, ITokenizer<string, int> sourceTokenizer, ITextCorpus sourceCorpus,
			Func<string, string> targetPreprocessor, ITokenizer<string, int> targetTokenizer, ITextCorpus targetCorpus, IProgress progress = null)
		{
			CheckDisposed();

			lock (_sessions)
			{
				if (_sessions.Count > 0)
					throw new InvalidOperationException("The engine cannot be trained while there are active sessions open.");

				Thot.decoder_close(_handle);
				TrainModels(_cfgFileName, sourcePreprocessor, sourceTokenizer, sourceCorpus, targetPreprocessor, targetTokenizer, targetCorpus, progress);
				_handle = Thot.decoder_open(_cfgFileName);
				_singleWordAlignmentModel.Handle = Thot.decoder_getSingleWordAlignmentModel(_handle);
				_inverseSingleWordAlignmentModel.Handle = Thot.decoder_getInverseSingleWordAlignmentModel(_handle);
			}
		}

		public void Train(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment, WordAlignmentMatrix matrix = null)
		{
			CheckDisposed();

			GlobalSession.Train(sourceSegment, targetSegment, matrix);
		}

		public TranslationResult Translate(IEnumerable<string> segment)
		{
			CheckDisposed();

			return GlobalSession.Translate(segment);
		}

		public IEnumerable<TranslationResult> Translate(int n, IEnumerable<string> segment)
		{
			CheckDisposed();

			return GlobalSession.Translate(n, segment);
		}

		public TranslationResult GetBestPhraseAlignment(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment)
		{
			CheckDisposed();

			return GlobalSession.GetBestPhraseAlignment(sourceSegment, targetSegment);
		}

		public IInteractiveTranslationSession StartSession()
		{
			CheckDisposed();

			lock (_sessions)
			{
				var session = new ThotSmtSession(this);
				_sessions.Add(session);
				return session;
			}
		}

		public void Save()
		{
			Thot.decoder_saveModels(_handle);
		}

		public ISegmentAligner SingleWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _singleWordAlignmentModel;
			}
		}

		public ISegmentAligner InverseSingleWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _inverseSingleWordAlignmentModel;
			}
		}

		internal IntPtr Handle => _handle;

		internal void RemoveSession(ThotSmtSession session)
		{
			lock (_sessions)
				_sessions.Remove(session);
		}

		protected override void DisposeManagedResources()
		{
			lock (_sessions)
			{
				foreach (ThotSmtSession session in _sessions.ToArray())
					session.Dispose();
			}
		}

		private ThotSmtSession GlobalSession
		{
			get
			{
				lock (_sessions)
				{
					if (_globalSession == null)
					{
						_globalSession = new ThotSmtSession(this);
						_sessions.Add(_globalSession);
					}
				}
				return _globalSession;
			}
		}

		protected override void DisposeUnmanagedResources()
		{
			Thot.decoder_close(_handle);
		}
	}
}
