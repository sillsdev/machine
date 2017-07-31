using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	internal class ThotSmtEngine : DisposableBase, IInteractiveSmtEngine
	{
		private readonly ThotSmtModel _smtModel;
		private readonly HashSet<ThotInteractiveTranslationSession> _sessions;
		private readonly ISegmentAligner _segmentAligner;
		private IntPtr _decoderHandle;

		public ThotSmtEngine(ThotSmtModel smtModel)
		{
			_smtModel = smtModel;
			_sessions = new HashSet<ThotInteractiveTranslationSession>();
			LoadHandle();
			_segmentAligner = new FuzzyEditDistanceSegmentAligner(GetTranslationProbability);
			ErrorCorrectionModel = new ErrorCorrectionModel();
		}

		internal void CloseHandle()
		{
			Thot.decoder_close(_decoderHandle);
		}

		internal void LoadHandle()
		{
			_decoderHandle = Thot.LoadDecoder(_smtModel.Handle, _smtModel.Parameters);
		}

		public TranslationResult Translate(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			return Thot.DoTranslate(_decoderHandle, Thot.decoder_translate, segment, false, segment, CreateResult);
		}

		public IEnumerable<TranslationResult> Translate(int n, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			return Thot.DoTranslateNBest(_decoderHandle, Thot.decoder_translateNBest, n, segment, false, segment, CreateResult);
		}

		public IInteractiveTranslationSession TranslateInteractively(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			var session = new ThotInteractiveTranslationSession(this, segment, GetWordGraph(segment));
			_sessions.Add(session);
			return session;
		}

		public WordGraph GetWordGraph(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			IntPtr nativeSentence = Thot.ConvertStringsToNativeUtf8(segment);
			IntPtr wordGraph = IntPtr.Zero;
			IntPtr nativeWordGraphStr = IntPtr.Zero;
			try
			{
				wordGraph = Thot.decoder_getWordGraph(_decoderHandle, nativeSentence);

				uint len = Thot.wg_getString(wordGraph, IntPtr.Zero, 0);
				nativeWordGraphStr = Marshal.AllocHGlobal((int) len);
				Thot.wg_getString(wordGraph, nativeWordGraphStr, len);
				string wordGraphStr = Thot.ConvertNativeUtf8ToString(nativeWordGraphStr, len);
				double initialStateScore = Thot.wg_getInitialStateScore(wordGraph);
				return CreateWordGraph(segment, wordGraphStr, initialStateScore);
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

		private WordGraph CreateWordGraph(IReadOnlyList<string> segment, string wordGraphStr, double initialStateScore)
		{
			string[] lines = wordGraphStr.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
			if (lines.Length == 0)
				return new WordGraph(Enumerable.Empty<WordGraphArc>(), Enumerable.Empty<int>(), initialStateScore);

			int i = 0;
			if (lines[i].StartsWith("#"))
				i++;

			int[] finalStates = Split(lines[i]).Select(int.Parse).ToArray();
			i++;

			string[] segmentArray = segment.ToArray();
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
					waMatrix = new WordAlignmentMatrix(1, 1) { [0, 0] = AlignmentType.Aligned };
				}
				else
				{
					var srcPhrase = new string[srcPhraseLen];
					Array.Copy(segmentArray, srcStartIndex, srcPhrase, 0, srcPhraseLen);
					waMatrix = _segmentAligner.GetBestAlignment(srcPhrase, words);
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
							double prob = isUnknown ? 0 : GetTranslationProbability(sourceWord, targetWord);
							totalProb += prob;
							alignedWordCount++;
						}
					}

					confidences[k] = alignedWordCount == 0
						? GetTranslationProbability(null, targetWord)
						: totalProb / alignedWordCount;
				}

				arcs.Add(new WordGraphArc(predStateIndex, succStateIndex, score, words, waMatrix, confidences, srcStartIndex,
					srcEndIndex, isUnknown));
			}

			return new WordGraph(arcs, finalStates, initialStateScore);
		}

		private double GetTranslationProbability(string sourceWord, string targetWord)
		{
			double prob = _smtModel.SingleWordAlignmentModel.GetTranslationProbability(sourceWord, targetWord);
			double invProb = _smtModel.InverseSingleWordAlignmentModel.GetTranslationProbability(targetWord, sourceWord);
			return Math.Max(prob, invProb);
		}

		private static string[] Split(string line)
		{
			return line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
		}

		public TranslationResult GetBestPhraseAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			IntPtr nativeSourceSegment = Thot.ConvertStringsToNativeUtf8(sourceSegment);
			IntPtr nativeTargetSegment = Thot.ConvertStringsToNativeUtf8(targetSegment);
			IntPtr data = IntPtr.Zero;
			try
			{
				data = Thot.decoder_getBestPhraseAlignment(_decoderHandle, nativeSourceSegment, nativeTargetSegment);
				return CreateResult(sourceSegment, targetSegment, data);
			}
			finally
			{
				if (data != IntPtr.Zero)
					Thot.tdata_destroy(data);
				Marshal.FreeHGlobal(nativeTargetSegment);
				Marshal.FreeHGlobal(nativeSourceSegment);
			}
		}

		public void TrainSegment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment,
			WordAlignmentMatrix matrix = null)
		{
			CheckDisposed();

			Thot.TrainSegmentPair(_decoderHandle, sourceSegment, targetSegment, matrix);
		}

		internal TranslationResult CreateResult(IReadOnlyList<string> sourceSegment, int prefixCount, TranslationInfo info)
		{
			if (info == null)
			{
				return new TranslationResult(sourceSegment, Enumerable.Empty<string>(), Enumerable.Empty<double>(),
					Enumerable.Empty<TranslationSources>(), new WordAlignmentMatrix(sourceSegment.Count, 0));
			}

			double[] confidences = info.TargetConfidences.ToArray();
			var sources = new TranslationSources[info.Target.Count];
			var alignment = new WordAlignmentMatrix(sourceSegment.Count, info.Target.Count);
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
							if (!info.TargetUnknownWords.Contains(j) && confidence < 0)
								prob = GetTranslationProbability(sourceWord, targetWord);
							alignment[i, j] = AlignmentType.Aligned;
							totalProb += prob;
							alignedWordCount++;
						}
					}

					if (confidence < 0)
					{
						confidences[j] = alignedWordCount == 0
							? GetTranslationProbability(null, targetWord)
							: totalProb / alignedWordCount;
					}

					if (j < prefixCount)
					{
						sources[j] = TranslationSources.Prefix;
						if (info.TargetUncorrectedPrefixWords.Contains(j))
							sources[j] |= TranslationSources.Smt;
					}
					else if (info.TargetUnknownWords.Contains(j))
					{
						sources[j] = TranslationSources.None;
					}
					else
					{
						sources[j] = TranslationSources.Smt;
					}
				}
				trgPhraseStartIndex = phrase.TargetCut + 1;
			}

			return new TranslationResult(sourceSegment, info.Target, confidences, sources, alignment);
		}

		private TranslationResult CreateResult(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment,
			IntPtr dataPtr)
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
					waMatrix = new WordAlignmentMatrix(1, 1) { [0, 0] = AlignmentType.Aligned };
				}
				else
				{
					var srcPhrase = new string[srcPhraseLen];
					for (int i = 0; i < srcPhraseLen; i++)
						srcPhrase[i] = sourceSegment[phrase.SourceStartIndex + i];
					var trgPhrase = new string[trgPhraseLen];
					for (int j = 0; j < trgPhraseLen; j++)
						trgPhrase[j] = targetSegment[trgPhraseStartIndex + j];
					waMatrix = _segmentAligner.GetBestAlignment(srcPhrase, trgPhrase);
				}
				phrase.Alignment = waMatrix;
				data.Phrases.Add(phrase);
				trgPhraseStartIndex += trgPhraseLen;
			}

			return CreateResult(sourceSegment, 0, data);
		}

		private IReadOnlyList<Tuple<int, int>> GetSourceSegmentation(IntPtr data, uint phraseCount)
		{
			int sizeOfPtr = Marshal.SizeOf(typeof(IntPtr));
			int sizeOfUInt = Marshal.SizeOf(typeof(uint));
			IntPtr nativeSourceSegmentation = Marshal.AllocHGlobal((int)phraseCount * sizeOfPtr);
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
					sourceSegmentation[i] = Tuple.Create(Marshal.ReadInt32(array, 0 * sizeOfUInt),
						Marshal.ReadInt32(array, 1 * sizeOfUInt));
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
			IntPtr nativeTargetSegmentCuts = Marshal.AllocHGlobal((int)phraseCount * sizeOfUInt);
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
				uint count = Thot.tdata_getTargetUnknownWords(data, nativeTargetUnknownWords, (uint)targetWordCount);
				for (int i = 0; i < count; i++)
					targetUnknownWords.Add(Marshal.ReadInt32(nativeTargetUnknownWords, i * sizeOfUInt));
			}
			finally
			{
				Marshal.FreeHGlobal(nativeTargetUnknownWords);
			}
		}

		internal void RemoveSession(ThotInteractiveTranslationSession session)
		{
			_sessions.Remove(session);
		}

		internal ErrorCorrectionModel ErrorCorrectionModel { get; }

		protected override void DisposeManagedResources()
		{
			foreach (ThotInteractiveTranslationSession session in _sessions.ToArray())
				session.Dispose();
			_smtModel.RemoveEngine(this);
		}

		protected override void DisposeUnmanagedResources()
		{
			Thot.decoder_close(_decoderHandle);
		}
	}
}
