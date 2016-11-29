using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	internal class ThotSmtSession : DisposableBase, IInteractiveTranslationSession
	{
		private readonly ThotSmtEngine _engine;
		private readonly IntPtr _handle;
		private readonly List<string> _sourceSegment; 
		private readonly ReadOnlyList<string> _readOnlySourceSegment;
		private readonly List<string> _prefix;
		private readonly ReadOnlyList<string> _readOnlyPrefix;
		private readonly ISegmentAligner _segmentAligner;
		private bool _isLastWordComplete;
		private TranslationResult _currentResult;
		private ErrorCorrectingWordGraphProcessor _wordGraphProcessor;

		public ThotSmtSession(ThotSmtEngine engine)
		{
			_engine = engine;
			_handle = Thot.decoder_openSession(_engine.Handle);
			_segmentAligner = new FuzzyEditDistanceSegmentAligner(_engine.SingleWordAlignmentModel);
			_sourceSegment = new List<string>();
			_readOnlySourceSegment = new ReadOnlyList<string>(_sourceSegment);
			_prefix = new List<string>();
			_readOnlyPrefix = new ReadOnlyList<string>(_prefix);
			_isLastWordComplete = true;
		}

		public TranslationResult Translate(IEnumerable<string> segment)
		{
			CheckDisposed();

			string[] segmentArray = segment.ToArray();
			return Thot.DoTranslate(_handle, Thot.session_translate, segmentArray, false, segmentArray, CreateResult);
		}

		public IEnumerable<TranslationResult> Translate(int n, IEnumerable<string> segment)
		{
			CheckDisposed();

			string[] segmentArray = segment.ToArray();
			return Thot.DoTranslateNBest(_handle, Thot.session_translateNBest, n, segmentArray, false, segmentArray, CreateResult);
		}

		public TranslationResult GetBestPhraseAlignment(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment)
		{
			CheckDisposed();

			string[] sourceSegmentArray = sourceSegment.ToArray();
			string[] targetSegmentArray = targetSegment.ToArray();
			IntPtr nativeSourceSegment = Thot.ConvertStringsToNativeUtf8(sourceSegmentArray);
			IntPtr nativeTargetSegment = Thot.ConvertStringsToNativeUtf8(targetSegmentArray);
			IntPtr data = IntPtr.Zero;
			try
			{
				data = Thot.session_getBestPhraseAlignment(_handle, nativeSourceSegment, nativeTargetSegment);
				return CreateResult(sourceSegmentArray, targetSegmentArray, data);
			}
			finally
			{
				if (data != IntPtr.Zero)
					Thot.tdata_destroy(data);
				Marshal.FreeHGlobal(nativeTargetSegment);
				Marshal.FreeHGlobal(nativeSourceSegment);
			}
		}

		public void Train(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment, WordAlignmentMatrix matrix = null)
		{
			CheckDisposed();

			Thot.TrainSegmentPair(_handle, sourceSegment, targetSegment, matrix);
		}

		public ReadOnlyList<string> SourceSegment
		{
			get
			{
				CheckDisposed();
				return _readOnlySourceSegment;
			}
		}

		public ReadOnlyList<string> Prefix
		{
			get
			{
				CheckDisposed();
				return _readOnlyPrefix;
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

		public TranslationResult TranslateInteractively(IEnumerable<string> segment)
		{
			CheckDisposed();

			Reset();
			_sourceSegment.AddRange(segment);

			WordGraph wordGraph = GetWordGraph();
			_wordGraphProcessor = new ErrorCorrectingWordGraphProcessor(_engine.ErrorCorrectingModel, wordGraph);

			_currentResult = CreateInteractiveResult();

			return _currentResult;
		}

		private WordGraph GetWordGraph()
		{
			IntPtr nativeSentence = Thot.ConvertStringsToNativeUtf8(_sourceSegment);
			IntPtr wordGraph = IntPtr.Zero;
			IntPtr nativeWordGraphStr = IntPtr.Zero;
			try
			{
				wordGraph = Thot.session_translateWordGraph(_handle, nativeSentence);

				uint len = Thot.wg_getString(wordGraph, IntPtr.Zero, 0);
				nativeWordGraphStr = Marshal.AllocHGlobal((int) len);
				Thot.wg_getString(wordGraph, nativeWordGraphStr, len);
				string wordGraphStr = Thot.ConvertNativeUtf8ToString(nativeWordGraphStr, len);
				double initialStateScore = Thot.wg_getInitialStateScore(wordGraph);
				return new WordGraph(wordGraphStr, initialStateScore);
			}
			finally
			{
				if (nativeWordGraphStr != IntPtr.Zero)
					Marshal.FreeHGlobal(nativeWordGraphStr);
				if (wordGraph != IntPtr.Zero)
					Thot.wg_destroy(wordGraph);
				Marshal.FreeHGlobal(nativeSentence);
			}
		}

		private TranslationResult CreateInteractiveResult()
		{
			TranslationData correction = _wordGraphProcessor.Correct(_prefix, _isLastWordComplete, 1).FirstOrDefault();
			if (correction == null)
				return new TranslationResult(_sourceSegment, Enumerable.Empty<string>(), Enumerable.Empty<double>(), new AlignedWordPair[0, 0]);

			return CreateResult(_sourceSegment, correction.Target, correction.SourceSegmentation, correction.TargetSegmentCuts, correction.TargetUnknownWords);
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

		public void Reset()
		{
			CheckDisposed();

			_sourceSegment.Clear();
			_prefix.Clear();
			_isLastWordComplete = true;
			_wordGraphProcessor = null;
			_currentResult = null;
		}

		public void Approve()
		{
			CheckDisposed();

			Train(_sourceSegment, _prefix);
		}

		private TranslationResult CreateResult(IList<string> sourceSegment, IList<string> targetSegment, IntPtr data)
		{
			uint phraseCount = Thot.tdata_getPhraseCount(data);
			IList<Tuple<int, int>> sourceSegmentation = GetSourceSegmentation(data, phraseCount);
			IList<int> targetSegmentCuts = GetTargetSegmentCuts(data, phraseCount);
			ISet<int> targetUnknownWords = GetTargetUnknownWords(data, targetSegment.Count);
			return CreateResult(sourceSegment, targetSegment, sourceSegmentation, targetSegmentCuts, targetUnknownWords);
		}

		private TranslationResult CreateResult(IList<string> sourceSegment, IList<string> targetSegment, IList<Tuple<int, int>> sourceSegmentation,
			IList<int> targetSegmentCuts, ISet<int> targetUnknownWords)
		{
			// TODO: find a better way to handle long untranslated suffixes

			var confidences = new double[targetSegment.Count];
			AlignedWordPair[,] alignment = new AlignedWordPair[sourceSegment.Count, targetSegment.Count];
			int trgPhraseStartIndex = 0;
			for (int k = 0; k < sourceSegmentation.Count; k++)
			{
				int srcStartIndex = sourceSegmentation[k].Item1 - 1;
				int srcEndIndex = sourceSegmentation[k].Item2 - 1;
				int trgCut = targetSegmentCuts[k] - 1;

				int srcPhraseLen = srcStartIndex - srcEndIndex + 1;
				int trgPhraseLen = trgCut - trgPhraseStartIndex + 1;
				WordAlignmentMatrix waMatrix;
				if (srcPhraseLen == 1 && trgPhraseLen == 1)
				{
					waMatrix = new WordAlignmentMatrix(1, 1) {[0, 0] = AlignmentType.Aligned};
				}
				else
				{
					var srcPhrase = new List<string>();
					for (int i = srcStartIndex; i <= srcEndIndex; i++)
						srcPhrase.Add(sourceSegment[i]);
					var trgPhrase = new List<string>();
					for (int j = trgPhraseStartIndex; j <= trgCut; j++)
						trgPhrase.Add(targetSegment[j]);
					_segmentAligner.GetBestAlignment(srcPhrase, trgPhrase, out waMatrix);
				}
				for (int j = trgPhraseStartIndex; j <= trgCut; j++)
				{
					string targetWord = targetSegment[j];
					double totalProb = 0;
					int alignedWordCount = 0;
					for (int i = srcStartIndex; i <= srcEndIndex; i++)
					{
						if (waMatrix[i - srcStartIndex, j - trgPhraseStartIndex] == AlignmentType.Aligned)
						{
							string sourceWord = sourceSegment[i];
							double prob = _segmentAligner.GetTranslationProbability(sourceWord, targetWord);
							TranslationSources sources = TranslationSources.Smt;
							if (targetUnknownWords.Contains(j + 1))
							{
								prob = 0;
								sources = TranslationSources.None;
							}
							alignment[i, j] = new AlignedWordPair(i, j, prob, sources);
							totalProb += prob;
							alignedWordCount++;
						}
					}

					confidences[j] = alignedWordCount == 0 ? _segmentAligner.GetTranslationProbability(null, targetWord) : totalProb / alignedWordCount;
				}
				trgPhraseStartIndex += trgPhraseLen;
			}
			return new TranslationResult(sourceSegment, targetSegment, confidences, alignment);
		}

		private IList<Tuple<int, int>> GetSourceSegmentation(IntPtr data, uint phraseCount)
		{
			int sizeOfPtr = Marshal.SizeOf(typeof(IntPtr));
			int sizeOfUInt = Marshal.SizeOf(typeof(uint));
			IntPtr nativeSourceSegmentation = Marshal.AllocHGlobal((int) phraseCount * sizeOfPtr);
			for (int i = 0; i < phraseCount; i++)
			{
				IntPtr array = Marshal.AllocHGlobal(2 * sizeOfUInt);
				Marshal.WriteIntPtr(nativeSourceSegmentation, i * sizeOfPtr, array);
			}

			try
			{
				Thot.tdata_getSourceSegmentation(data, nativeSourceSegmentation, phraseCount);
				var sourceSegmentation = new Tuple<int, int>[phraseCount];
				for (int i = 0; i < phraseCount; i++)
				{
					IntPtr array = Marshal.ReadIntPtr(nativeSourceSegmentation, i * sizeOfPtr);
					sourceSegmentation[i] = Tuple.Create(Marshal.ReadInt32(array, 0 * sizeOfUInt), Marshal.ReadInt32(array, 1 * sizeOfUInt));
				}
				return sourceSegmentation;
			}
			finally
			{
				for (int i = 0; i < phraseCount; i++)
				{
					IntPtr array = Marshal.ReadIntPtr(nativeSourceSegmentation, i * sizeOfPtr);
					Marshal.FreeHGlobal(array);
				}
				Marshal.FreeHGlobal(nativeSourceSegmentation);
			}
		}

		private IList<int> GetTargetSegmentCuts(IntPtr data, uint phraseCount)
		{
			int sizeOfUInt = Marshal.SizeOf(typeof(uint));
			IntPtr nativeTargetSegmentCuts = Marshal.AllocHGlobal((int) phraseCount * sizeOfUInt);
			try
			{
				Thot.tdata_getTargetSegmentCuts(data, nativeTargetSegmentCuts, phraseCount);
				var targetSegmentCuts = new int[phraseCount];
				for (int i = 0; i < phraseCount; i++)
					targetSegmentCuts[i] = Marshal.ReadInt32(nativeTargetSegmentCuts, i * sizeOfUInt);
				return targetSegmentCuts;
			}
			finally
			{
				Marshal.FreeHGlobal(nativeTargetSegmentCuts);
			}
		}

		private ISet<int> GetTargetUnknownWords(IntPtr data, int targetWordCount)
		{
			int sizeOfUInt = Marshal.SizeOf(typeof(uint));
			IntPtr nativeTargetUnknownWords = Marshal.AllocHGlobal(targetWordCount * sizeOfUInt);
			try
			{
				uint count = Thot.tdata_getTargetUnknownWords(data, nativeTargetUnknownWords, (uint) targetWordCount);
				var targetUnknownWords = new HashSet<int>();
				for (int i = 0; i < count; i++)
					targetUnknownWords.Add(Marshal.ReadInt32(nativeTargetUnknownWords, i * sizeOfUInt));
				return targetUnknownWords;
			}
			finally
			{
				Marshal.FreeHGlobal(nativeTargetUnknownWords);
			}
		}

		protected override void DisposeManagedResources()
		{
			_engine.RemoveSession(this);
		}

		protected override void DisposeUnmanagedResources()
		{
			Thot.session_close(_handle);
		}
	}
}
