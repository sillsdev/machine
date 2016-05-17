using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	internal class ThotSmtSession : DisposableBase, ISmtSession
	{
		private delegate int TranslateFunc(IntPtr sessionHandle, IntPtr sourceSegment, IntPtr result, int capacity, out IntPtr data);

		private const int DefaultTranslationBufferLength = 1024;

		private readonly ThotSmtEngine _engine;
		private readonly IntPtr _handle;
		private readonly List<string> _sourceSegment; 
		private readonly ReadOnlyList<string> _readOnlySourceSegment;
		private readonly List<string> _prefix;
		private readonly ReadOnlyList<string> _readOnlyPrefix;
		private readonly ISegmentAligner _segmentAligner;
		private bool _isLastWordPartial;
		private bool _isTranslatingInteractively;

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
			return DoTranslate(Thot.session_translate, segmentArray, false, segmentArray);
		}

		public IReadOnlyList<string> SourceSegment
		{
			get { return _readOnlySourceSegment; }
		}

		public IReadOnlyList<string> Prefix
		{
			get { return _readOnlyPrefix; }
		}

		public bool IsLastWordPartial
		{
			get { return _isLastWordPartial; }
		}

		public TranslationResult TranslateInteractively(IEnumerable<string> segment)
		{
			CheckDisposed();

			Reset();
			_sourceSegment.AddRange(segment);
			_isTranslatingInteractively = true;
			return DoTranslate(Thot.session_translateInteractively, _sourceSegment, false, _sourceSegment);
		}

		public TranslationResult AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			CheckDisposed();
			if (!_isTranslatingInteractively)
				throw new InvalidOperationException("An interactive translation has not been started.");

			string[] additionArray = addition.ToArray();
			_prefix.AddRange(additionArray);
			return DoTranslate(Thot.session_addStringToPrefix, additionArray, !isLastWordPartial, _sourceSegment);
		}

		public TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			CheckDisposed();
			if (!_isTranslatingInteractively)
				throw new InvalidOperationException("An interactive translation has not been started.");

			_prefix.Clear();
			_prefix.AddRange(prefix);
			_isLastWordPartial = isLastWordPartial;
			return DoTranslate(Thot.session_setPrefix, _prefix, !isLastWordPartial, _sourceSegment);
		}

		public void Reset()
		{
			_sourceSegment.Clear();
			_prefix.Clear();
			_isLastWordPartial = true;
			_isTranslatingInteractively = false;
		}

		public void Approve()
		{
			Train(_sourceSegment, _prefix);
		}

		private TranslationResult DoTranslate(TranslateFunc translateFunc, IEnumerable<string> input, bool addTrailingSpace, IList<string> sourceSegment)
		{
			IntPtr inputPtr = Thot.ConvertStringToNativeUtf8(string.Join(" ", input) + (addTrailingSpace ? " " : ""));
			IntPtr translationPtr = Marshal.AllocHGlobal(DefaultTranslationBufferLength);
			IntPtr data = IntPtr.Zero;
			try
			{
				int len = translateFunc(_handle, inputPtr, translationPtr, DefaultTranslationBufferLength, out data);
				if (len > DefaultTranslationBufferLength)
				{
					Thot.tdata_destroy(data);
					translationPtr = Marshal.ReAllocHGlobal(translationPtr, (IntPtr) len);
					len = translateFunc(_handle, inputPtr, translationPtr, len, out data);
				}
				string translation = Thot.ConvertNativeUtf8ToString(translationPtr, len);
				string[] targetSegment = translation.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
				return CreateResult(sourceSegment, targetSegment, data);
			}
			finally
			{
				if (data != IntPtr.Zero)
					Thot.tdata_destroy(data);
				Marshal.FreeHGlobal(translationPtr);
				Marshal.FreeHGlobal(inputPtr);
			}
		}

		private TranslationResult CreateResult(IList<string> sourceSegment, IList<string> targetSegment, IntPtr data)
		{
			// TODO: find a better way to handle long untranslated suffixes

			int phraseCount = Thot.tdata_getPhraseCount(data);
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
					waMatrix = new WordAlignmentMatrix(1, 1);
					waMatrix[0, 0] = true;
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
						if (waMatrix[i - sourceSegmentation[k].Item1, j - trgPhraseStartIndex])
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

		private IList<Tuple<int, int>> GetSourceSegmentation(IntPtr data, int phraseCount)
		{
			int sizeOfPtr = Marshal.SizeOf(typeof(IntPtr));
			int sizeOfInt = Marshal.SizeOf(typeof(int));
			IntPtr nativeSourceSegmentation = Marshal.AllocHGlobal(phraseCount * sizeOfPtr);
			for (int i = 0; i < phraseCount; i++)
			{
				IntPtr array = Marshal.AllocHGlobal(2 * sizeOfInt);
				Marshal.WriteIntPtr(nativeSourceSegmentation, i * sizeOfPtr, array);
			}

			try
			{
				Thot.tdata_getSourceSegmentation(data, nativeSourceSegmentation, phraseCount);
				var sourceSegmentation = new Tuple<int, int>[phraseCount];
				for (int i = 0; i < phraseCount; i++)
				{
					IntPtr array = Marshal.ReadIntPtr(nativeSourceSegmentation, i * sizeOfPtr);
					sourceSegmentation[i] = Tuple.Create(Marshal.ReadInt32(array, 0 * sizeOfInt) - 1, Marshal.ReadInt32(array, 1 * sizeOfInt) - 1);
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

		private IList<int> GetTargetSegmentCuts(IntPtr data, int phraseCount)
		{
			int sizeOfInt = Marshal.SizeOf(typeof(int));
			IntPtr nativeTargetSegmentCuts = Marshal.AllocHGlobal(phraseCount * sizeOfInt);
			try
			{
				Thot.tdata_getTargetSegmentCuts(data, nativeTargetSegmentCuts, phraseCount);
				var targetSegmentCuts = new int[phraseCount];
				for (int i = 0; i < phraseCount; i++)
					targetSegmentCuts[i] = Marshal.ReadInt32(nativeTargetSegmentCuts, i * sizeOfInt) - 1;
				return targetSegmentCuts;
			}
			finally
			{
				Marshal.FreeHGlobal(nativeTargetSegmentCuts);
			}
		}

		private ISet<int> GetTargetUnknownWords(IntPtr data, int targetWordCount)
		{
			int sizeOfInt = Marshal.SizeOf(typeof(int));
			IntPtr nativeTargetUnknownWords = Marshal.AllocHGlobal(targetWordCount * sizeOfInt);
			try
			{
				int count = Thot.tdata_getTargetUnknownWords(data, nativeTargetUnknownWords, targetWordCount);
				var targetUnknownWords = new HashSet<int>();
				for (int i = 0; i < count; i++)
					targetUnknownWords.Add(Marshal.ReadInt32(nativeTargetUnknownWords, i * sizeOfInt) - 1);
				return targetUnknownWords;
			}
			finally
			{
				Marshal.FreeHGlobal(nativeTargetUnknownWords);
			}
		}

		public void Train(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment)
		{
			CheckDisposed();

			IntPtr nativeSourceSegment = Thot.ConvertStringsToNativeUtf8(sourceSegment);
			IntPtr nativeTargetSegment = Thot.ConvertStringsToNativeUtf8(targetSegment);
			try
			{
				Thot.session_trainSentencePair(_handle, nativeSourceSegment, nativeTargetSegment);
			}
			finally
			{
				Marshal.FreeHGlobal(nativeTargetSegment);
				Marshal.FreeHGlobal(nativeSourceSegment);
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
