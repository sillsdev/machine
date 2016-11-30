using System;
using System.Collections.Generic;
using System.Globalization;
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

		public WordGraph GetWordGraph(IEnumerable<string> segment)
		{
			CheckDisposed();

			string[] segmentArray = segment.ToArray();

			IntPtr nativeSentence = Thot.ConvertStringsToNativeUtf8(segmentArray);
			IntPtr wordGraph = IntPtr.Zero;
			IntPtr nativeWordGraphStr = IntPtr.Zero;
			try
			{
				wordGraph = Thot.session_translateWordGraph(_handle, nativeSentence);

				uint len = Thot.wg_getString(wordGraph, IntPtr.Zero, 0);
				nativeWordGraphStr = Marshal.AllocHGlobal((int)len);
				Thot.wg_getString(wordGraph, nativeWordGraphStr, len);
				string wordGraphStr = Thot.ConvertNativeUtf8ToString(nativeWordGraphStr, len);
				double initialStateScore = Thot.wg_getInitialStateScore(wordGraph);
				return CreateWordGraph(segmentArray, wordGraphStr, initialStateScore);
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

		private WordGraph CreateWordGraph(string[] segment, string wordGraphStr, double initialStateScore)
		{
			string[] lines = wordGraphStr.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
			int i = 0;
			if (lines[i].StartsWith("#"))
				i++;

			int[] finalStates = Split(lines[i]).Select(s => int.Parse(s)).ToArray();
			i++;

			var arcs = new List<WordGraphArc>();
			for (; i < lines.Length; i++)
			{
				string[] arcParts = Split(lines[i]);

				if (arcParts.Length < 6)
					continue;

				int predStateIndex = int.Parse(arcParts[0]);
				int succStateIndex = int.Parse(arcParts[1]);
				double score = double.Parse(arcParts[2], CultureInfo.InvariantCulture);
				int srcStartIndex = int.Parse(arcParts[3]) - 1;
				int srcEndIndex = int.Parse(arcParts[4]) - 1;
				bool isUnknown = arcParts[5] == "1";

				int j = 6;
				if (arcParts.Length >= 7)
				{
					if (arcParts[j] == "|||")
					{
						j++;
						while (j < arcParts.Length && arcParts[j] != "|||")
							j++;
						j++;
					}
				}

				int trgPhraseLen = arcParts.Length - j;
				var words = new string[trgPhraseLen];
				Array.Copy(arcParts, j, words, 0, trgPhraseLen);

				int srcPhraseLen = srcEndIndex - srcStartIndex + 1;
				WordAlignmentMatrix waMatrix;
				if (srcPhraseLen == 1 && trgPhraseLen == 1)
				{
					waMatrix = new WordAlignmentMatrix(1, 1) {[0, 0] = AlignmentType.Aligned};
				}
				else
				{
					var srcPhrase = new string[srcPhraseLen];
					Array.Copy(segment, srcStartIndex, srcPhrase, 0, srcPhraseLen);
					_segmentAligner.GetBestAlignment(srcPhrase, words, out waMatrix);
				}

				var confidences = new double[trgPhraseLen];
				for (int k = 0; k < trgPhraseLen; k++)
				{
					string targetWord = words[k];
					double totalProb = 0;
					int alignedWordCount = 0;
					for (int l = 0; l < srcPhraseLen; l++)
					{
						if (waMatrix[l, k] == AlignmentType.Aligned)
						{
							string sourceWord = segment[srcStartIndex + l];
							double prob = isUnknown ? 0 : _segmentAligner.GetTranslationProbability(sourceWord, targetWord);
							totalProb += prob;
							alignedWordCount++;
						}
					}

					confidences[k] = alignedWordCount == 0 ? _segmentAligner.GetTranslationProbability(null, targetWord) : totalProb / alignedWordCount;
				}

				arcs.Add(new WordGraphArc(predStateIndex, succStateIndex, score, words, waMatrix, confidences, srcStartIndex, srcEndIndex, isUnknown));
			}

			return new WordGraph(arcs, finalStates, initialStateScore);
		}

		private static string[] Split(string line)
		{
			return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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

		public IReadOnlyList<string> SourceSegment
		{
			get
			{
				CheckDisposed();
				return _readOnlySourceSegment;
			}
		}

		public IReadOnlyList<string> Prefix
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

			WordGraph wordGraph = GetWordGraph(_sourceSegment);
			_wordGraphProcessor = new ErrorCorrectingWordGraphProcessor(_engine.ErrorCorrectingModel, wordGraph);

			_currentResult = CreateInteractiveResult();

			return _currentResult;
		}

		private TranslationResult CreateInteractiveResult()
		{
			TranslationInfo correction = _wordGraphProcessor.Correct(_prefix, _isLastWordComplete, 1).FirstOrDefault();
			return CreateResult(_sourceSegment, correction);
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

		private TranslationResult CreateResult(IReadOnlyList<string> sourceSegment, TranslationInfo info)
		{
			if (info == null)
				return new TranslationResult(sourceSegment, Enumerable.Empty<string>(), Enumerable.Empty<double>(), new AlignedWordPair[0, 0]);

			double[] confidences = info.TargetConfidences.ToArray();
			AlignedWordPair[,] alignment = new AlignedWordPair[sourceSegment.Count, info.Target.Count];
			int trgPhraseStartIndex = 0;
			foreach (PhraseInfo phrase in info.Phrases)
			{
				for (int j = trgPhraseStartIndex; j <= phrase.TargetCut; j++)
				{
					string targetWord = info.Target[j];
					double confidence = info.TargetConfidences[j];
					double totalProb = 0;
					int alignedWordCount = 0;
					for (int i = phrase.SourceStartIndex; i <= phrase.SourceEndIndex; i++)
					{
						if (phrase.Alignment[i - phrase.SourceStartIndex, j - trgPhraseStartIndex] == AlignmentType.Aligned)
						{
							string sourceWord = sourceSegment[i];
							double prob = 0;
							TranslationSources sources = TranslationSources.None;
							if (!info.TargetUnknownWords.Contains(j))
							{
								if (confidence < 0)
									prob = _segmentAligner.GetTranslationProbability(sourceWord, targetWord);
								sources = TranslationSources.Smt;
							}
							alignment[i, j] = new AlignedWordPair(i, j, sources);
							totalProb += prob;
							alignedWordCount++;
						}
					}

					if (confidence < 0)
						confidences[j] = alignedWordCount == 0 ? _segmentAligner.GetTranslationProbability(null, targetWord) : totalProb / alignedWordCount;
				}
				trgPhraseStartIndex = phrase.TargetCut + 1;
			}

			return new TranslationResult(sourceSegment, info.Target, confidences, alignment);
		}

		private TranslationResult CreateResult(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment, IntPtr dataPtr)
		{
			var data = new TranslationInfo();
			foreach (string targetWord in targetSegment)
			{
				data.Target.Add(targetWord);
				data.TargetConfidences.Add(-1);
			}

			uint phraseCount = Thot.tdata_getPhraseCount(dataPtr);
			IReadOnlyList<Tuple<int, int>> sourceSegmentation = GetSourceSegmentation(dataPtr, phraseCount);
			IReadOnlyList<int> targetSegmentCuts = GetTargetSegmentCuts(dataPtr, phraseCount);
			GetTargetUnknownWords(dataPtr, targetSegment.Count, data.TargetUnknownWords);

			int trgPhraseStartIndex = 0;
			for (int k = 0; k < phraseCount; k++)
			{
				var phrase = new PhraseInfo
				{
					SourceStartIndex = sourceSegmentation[k].Item1 - 1,
					SourceEndIndex = sourceSegmentation[k].Item2 - 1,
					TargetCut = targetSegmentCuts[k] - 1
				};

				int srcPhraseLen = phrase.SourceEndIndex - phrase.SourceStartIndex + 1;
				int trgPhraseLen = phrase.TargetCut - trgPhraseStartIndex + 1;
				WordAlignmentMatrix waMatrix;
				if (srcPhraseLen == 1 && trgPhraseLen == 1)
				{
					waMatrix = new WordAlignmentMatrix(1, 1) {[0, 0] = AlignmentType.Aligned};
				}
				else
				{
					var srcPhrase = new string[srcPhraseLen];
					for (int i = 0; i < srcPhraseLen; i++)
						srcPhrase[i] = sourceSegment[phrase.SourceStartIndex + i];
					var trgPhrase = new string[trgPhraseLen];
					for (int j = 0; j < trgPhraseLen; j++)
						trgPhrase[j] = targetSegment[trgPhraseStartIndex + j];
					_segmentAligner.GetBestAlignment(srcPhrase, trgPhrase, out waMatrix);
				}
				phrase.Alignment = waMatrix;
				data.Phrases.Add(phrase);
				trgPhraseStartIndex += trgPhraseLen;
			}

			return CreateResult(sourceSegment, data);
		}

		private IReadOnlyList<Tuple<int, int>> GetSourceSegmentation(IntPtr data, uint phraseCount)
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

		private IReadOnlyList<int> GetTargetSegmentCuts(IntPtr data, uint phraseCount)
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

		private void GetTargetUnknownWords(IntPtr data, int targetWordCount, ISet<int> targetUnknownWords)
		{
			int sizeOfUInt = Marshal.SizeOf(typeof(uint));
			IntPtr nativeTargetUnknownWords = Marshal.AllocHGlobal(targetWordCount * sizeOfUInt);
			try
			{
				uint count = Thot.tdata_getTargetUnknownWords(data, nativeTargetUnknownWords, (uint) targetWordCount);
				for (int i = 0; i < count; i++)
					targetUnknownWords.Add(Marshal.ReadInt32(nativeTargetUnknownWords, i * sizeOfUInt));
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
