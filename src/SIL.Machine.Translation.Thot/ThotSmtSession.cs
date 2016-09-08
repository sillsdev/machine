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
		private bool _isLastWordPartial;
		private bool _isTranslatingInteractively;
		private TranslationResult _currentResult;

		public ThotSmtSession(ThotSmtEngine engine)
		{
			_engine = engine;
			_handle = Thot.decoder_openSession(_engine.Handle);
			_segmentAligner = new FuzzyEditDistanceSegmentAligner(_engine.SingleWordAlignmentModel);
			_sourceSegment = new List<string>();
			_readOnlySourceSegment = new ReadOnlyList<string>(_sourceSegment);
			_prefix = new List<string>();
			_readOnlyPrefix = new ReadOnlyList<string>(_prefix);
			_isLastWordPartial = true;
		}

		public TranslationResult Translate(IEnumerable<string> segment)
		{
			CheckDisposed();

			string[] segmentArray = segment.ToArray();
			return ThotSmtEngine.DoTranslate(_handle, Thot.session_translate, segmentArray, false, segmentArray, CreateResult);
		}

		public IEnumerable<TranslationResult> Translate(int n, IEnumerable<string> segment)
		{
			CheckDisposed();

			string[] segmentArray = segment.ToArray();
			return ThotSmtEngine.DoTranslateNBest(_handle, Thot.session_translateNBest, n, segmentArray, false, segmentArray, CreateResult);
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

			ThotSmtEngine.TrainSegmentPair(_handle, sourceSegment, targetSegment, matrix);
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

		public bool IsLastWordPartial
		{
			get
			{
				CheckDisposed();
				return _isLastWordPartial;
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
			_isTranslatingInteractively = true;
			_currentResult = ThotSmtEngine.DoTranslate(_handle, Thot.session_translateInteractively, _sourceSegment, false, _sourceSegment, CreateResult);
			return _currentResult;
		}

		public TranslationResult AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			CheckDisposed();
			if (!_isTranslatingInteractively)
				throw new InvalidOperationException("An interactive translation has not been started.");

			string[] additionArray = addition.ToArray();
			_prefix.AddRange(additionArray);
			_currentResult = ThotSmtEngine.DoTranslate(_handle, Thot.session_addStringToPrefix, additionArray, !isLastWordPartial, _sourceSegment, CreateResult);
			return _currentResult;
		}

		public TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			CheckDisposed();
			if (!_isTranslatingInteractively)
				throw new InvalidOperationException("An interactive translation has not been started.");

			_prefix.Clear();
			_prefix.AddRange(prefix);
			_isLastWordPartial = isLastWordPartial;
			_currentResult = ThotSmtEngine.DoTranslate(_handle, Thot.session_setPrefix, _prefix, !isLastWordPartial, _sourceSegment, CreateResult);
			return _currentResult;
		}

		public void Reset()
		{
			CheckDisposed();

			_sourceSegment.Clear();
			_prefix.Clear();
			_isLastWordPartial = true;
			_isTranslatingInteractively = false;
			_currentResult = null;
		}

		public void Approve()
		{
			CheckDisposed();

			Train(_sourceSegment, _prefix);
		}

		private TranslationResult CreateResult(IList<string> sourceSegment, IList<string> targetSegment, IntPtr data)
		{
			// TODO: find a better way to handle long untranslated suffixes

			uint phraseCount = Thot.tdata_getPhraseCount(data);
			IList<Tuple<int, int>> sourceSegmentation = GetSourceSegmentation(data, phraseCount);
			IList<int> targetSegmentCuts = GetTargetSegmentCuts(data, phraseCount);
			ISet<int> targetUnknownWords = GetTargetUnknownWords(data, targetSegment.Count);

			var confidences = new double[targetSegment.Count];
			AlignedWordPair[,] alignment = new AlignedWordPair[sourceSegment.Count, targetSegment.Count];
			int trgPhraseStartIndex = 0;
			for (int k = 0; k < phraseCount; k++)
			{
				int srcPhraseLen = sourceSegmentation[k].Item2 - sourceSegmentation[k].Item1 + 1;
				int trgPhraseLen = targetSegmentCuts[k] - trgPhraseStartIndex + 1;
				WordAlignmentMatrix waMatrix;
				if (srcPhraseLen == 1 && trgPhraseLen == 1)
				{
					waMatrix = new WordAlignmentMatrix(1, 1) {[0, 0] = AlignmentType.Aligned};
				}
				else
				{
					var srcPhrase = new List<string>();
					for (int i = sourceSegmentation[k].Item1; i <= sourceSegmentation[k].Item2; i++)
						srcPhrase.Add(sourceSegment[i]);
					var trgPhrase = new List<string>();
					for (int j = trgPhraseStartIndex; j <= targetSegmentCuts[k]; j++)
						trgPhrase.Add(targetSegment[j]);
					_segmentAligner.GetBestAlignment(srcPhrase, trgPhrase, out waMatrix);
				}
				for (int j = trgPhraseStartIndex; j <= targetSegmentCuts[k]; j++)
				{
					string targetWord = targetSegment[j];
					double totalProb = 0;
					int alignedWordCount = 0;
					for (int i = sourceSegmentation[k].Item1; i <= sourceSegmentation[k].Item2; i++)
					{
						if (waMatrix[i - sourceSegmentation[k].Item1, j - trgPhraseStartIndex] == AlignmentType.Aligned)
						{
							string sourceWord = sourceSegment[i];
							double prob = _segmentAligner.GetTranslationProbability(sourceWord, targetWord);
							TranslationSources sources = TranslationSources.Smt;
							if (targetUnknownWords.Contains(j))
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
					sourceSegmentation[i] = Tuple.Create(Marshal.ReadInt32(array, 0 * sizeOfUInt) - 1, Marshal.ReadInt32(array, 1 * sizeOfUInt) - 1);
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
					targetSegmentCuts[i] = Marshal.ReadInt32(nativeTargetSegmentCuts, i * sizeOfUInt) - 1;
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
					targetUnknownWords.Add(Marshal.ReadInt32(nativeTargetUnknownWords, i * sizeOfUInt) - 1);
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
