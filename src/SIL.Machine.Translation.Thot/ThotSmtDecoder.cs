using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.Machine.Annotations;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
    public class ThotSmtDecoder : DisposableBase
    {
        private readonly ThotSmtModel _smtModel;
        private readonly IWordConfidenceEstimator _confidenceEstimator;
        private IntPtr _decoderHandle;

        internal ThotSmtDecoder(ThotSmtModel smtModel)
        {
            _smtModel = smtModel;
            LoadHandle();
            _confidenceEstimator = new Ibm1WordConfidenceEstimator(
                _smtModel.SymmetrizedWordAlignmentModel.GetTranslationScore
            );
            //_confidenceEstimator = new WppWordConfidenceEstimator(this);
        }

        internal void CloseHandle()
        {
            Thot.decoder_close(_decoderHandle);
        }

        internal void LoadHandle()
        {
            _decoderHandle = Thot.LoadDecoder(_smtModel.Handle, _smtModel.Parameters);
        }

        public TranslationResult Translate(string segment)
        {
            CheckDisposed();

            return Translate(TokenizeSource(segment));
        }

        public TranslationResult Translate(IReadOnlyList<string> sourceTokens)
        {
            CheckDisposed();

            IReadOnlyList<string> normalizedSourceTokens = NormalizeSource(sourceTokens);

            return Thot.DoTranslate(
                _decoderHandle,
                Thot.decoder_translate,
                normalizedSourceTokens,
                (normalizedTargetTokens, data) =>
                    CreateResult(
                        sourceTokens,
                        normalizedSourceTokens,
                        DenormalizeTarget(normalizedTargetTokens),
                        normalizedTargetTokens,
                        data
                    )
            );
        }

        public IReadOnlyList<TranslationResult> Translate(int n, string segment)
        {
            CheckDisposed();

            return Translate(n, TokenizeSource(segment));
        }

        public IReadOnlyList<TranslationResult> Translate(int n, IReadOnlyList<string> sourceTokens)
        {
            CheckDisposed();

            IReadOnlyList<string> normalizedSourceTokens = NormalizeSource(sourceTokens);

            return Thot.DoTranslateNBest(
                _decoderHandle,
                Thot.decoder_translateNBest,
                n,
                normalizedSourceTokens,
                (normalizedTargetTokens, data) =>
                    CreateResult(
                        sourceTokens,
                        normalizedSourceTokens,
                        DenormalizeTarget(normalizedTargetTokens),
                        normalizedTargetTokens,
                        data
                    )
            );
        }

        public WordGraph GetWordGraph(string segment)
        {
            CheckDisposed();

            return GetWordGraph(TokenizeSource(segment));
        }

        public WordGraph GetWordGraph(IReadOnlyList<string> sourceTokens)
        {
            CheckDisposed();

            IReadOnlyList<string> normalizedSourceTokens = NormalizeSource(sourceTokens);
            IntPtr nativeSentence = Thot.ConvertSegmentToNativeUtf8(normalizedSourceTokens);
            IntPtr wordGraph = IntPtr.Zero;
            IntPtr nativeWordGraphStr = IntPtr.Zero;
            try
            {
                wordGraph = Thot.decoder_getWordGraph(_decoderHandle, nativeSentence);

                uint len = Thot.wg_getString(wordGraph, IntPtr.Zero, 0);
                nativeWordGraphStr = Marshal.AllocHGlobal((int)len);
                Thot.wg_getString(wordGraph, nativeWordGraphStr, len);
                string wordGraphStr = Thot.ConvertNativeUtf8ToString(nativeWordGraphStr, len);
                double initialStateScore = Thot.wg_getInitialStateScore(wordGraph);
                return CreateWordGraph(sourceTokens, normalizedSourceTokens, wordGraphStr, initialStateScore);
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

        private WordGraph CreateWordGraph(
            IReadOnlyList<string> sourceWords,
            IReadOnlyList<string> normalizedSourceWords,
            string wordGraphStr,
            double initialStateScore
        )
        {
            string[] lines = wordGraphStr.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                return new WordGraph(
                    sourceWords,
                    Enumerable.Empty<WordGraphArc>(),
                    Enumerable.Empty<int>(),
                    initialStateScore
                );
            }

            int i = 0;
            if (lines[i].StartsWith("#"))
                i++;

            int[] finalStates = Split(lines[i]).Select(int.Parse).ToArray();
            i++;

            string[] normalizedSourceWordsArray = normalizedSourceWords.ToArray();
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
                var normalizedWords = new string[trgPhraseLen];
                for (int k = 0; k < trgPhraseLen; k++)
                    normalizedWords[k] = Thot.UnescapeToken(arcParts[j + k]);

                int srcPhraseLen = srcEndIndex - srcStartIndex + 1;
                WordAlignmentMatrix waMatrix;
                if (srcPhraseLen == 1 && trgPhraseLen == 1)
                {
                    waMatrix = new WordAlignmentMatrix(1, 1) { [0, 0] = true };
                }
                else
                {
                    var srcPhrase = new string[srcPhraseLen];
                    Array.Copy(normalizedSourceWordsArray, srcStartIndex, srcPhrase, 0, srcPhraseLen);
                    waMatrix = _smtModel.WordAligner.Align(srcPhrase, normalizedWords);
                }

                var sources = new TranslationSources[normalizedWords.Length];
                for (int k = 0; k < sources.Length; k++)
                    sources[k] = isUnknown ? TranslationSources.None : TranslationSources.Smt;
                arcs.Add(
                    new WordGraphArc(
                        predStateIndex,
                        succStateIndex,
                        score,
                        DenormalizeTarget(normalizedWords),
                        waMatrix,
                        Range<int>.Create(srcStartIndex, srcEndIndex + 1),
                        sources
                    )
                );
            }

            var wordGraph = new WordGraph(sourceWords, arcs, finalStates, initialStateScore);
            _confidenceEstimator.Estimate(wordGraph);
            return wordGraph;
        }

        private static string[] Split(string line)
        {
            return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public TranslationResult GetBestPhraseAlignment(string sourceSegment, string targetSegment)
        {
            CheckDisposed();

            return GetBestPhraseAlignment(TokenizeSource(sourceSegment), TokenizeTarget(targetSegment));
        }

        public TranslationResult GetBestPhraseAlignment(
            IReadOnlyList<string> sourceTokens,
            IReadOnlyList<string> targetTokens
        )
        {
            CheckDisposed();

            IReadOnlyList<string> normalizedSourceTokens = NormalizeSource(sourceTokens);
            IReadOnlyList<string> normalizedTargetTokens = NormalizeTarget(targetTokens);

            IntPtr nativeSourceSegment = Thot.ConvertSegmentToNativeUtf8(normalizedSourceTokens);
            IntPtr nativeTargetSegment = Thot.ConvertSegmentToNativeUtf8(normalizedTargetTokens);
            IntPtr data = IntPtr.Zero;
            try
            {
                data = Thot.decoder_getBestPhraseAlignment(_decoderHandle, nativeSourceSegment, nativeTargetSegment);
                return CreateResult(sourceTokens, normalizedSourceTokens, targetTokens, normalizedTargetTokens, data);
            }
            finally
            {
                if (data != IntPtr.Zero)
                    Thot.tdata_destroy(data);
                Marshal.FreeHGlobal(nativeTargetSegment);
                Marshal.FreeHGlobal(nativeSourceSegment);
            }
        }

        public void TrainSegment(string sourceSegment, string targetSegment, bool sentenceStart = true)
        {
            CheckDisposed();

            TrainSegment(TokenizeSource(sourceSegment), TokenizeTarget(targetSegment), sentenceStart);
        }

        public void TrainSegment(
            IReadOnlyList<string> sourceTokens,
            IReadOnlyList<string> targetTokens,
            bool sentenceStart = true
        )
        {
            CheckDisposed();

            IReadOnlyList<string> normalizedTargetTokens = NormalizeTarget(targetTokens);

            Thot.TrainSegmentPair(_decoderHandle, NormalizeSource(sourceTokens), normalizedTargetTokens);

            _smtModel.Truecaser?.TrainSegment(targetTokens, sentenceStart);
        }

        private TranslationResult CreateResult(
            IReadOnlyList<string> sourceTokens,
            IReadOnlyList<string> normalizedSourceTokens,
            IReadOnlyList<string> targetTokens,
            IReadOnlyList<string> normalizedTargetTokens,
            IntPtr dataPtr
        )
        {
            var builder = new TranslationResultBuilder();

            uint phraseCount = Thot.tdata_getPhraseCount(dataPtr);
            IReadOnlyList<Tuple<int, int>> sourceSegmentation = GetSourceSegmentation(dataPtr, phraseCount);
            IReadOnlyList<int> targetSegmentCuts = GetTargetSegmentCuts(dataPtr, phraseCount);
            ISet<int> targetUnknownWords = GetTargetUnknownWords(dataPtr, targetTokens.Count);

            int trgPhraseStartIndex = 0;
            for (int k = 0; k < phraseCount; k++)
            {
                int sourceStartIndex = sourceSegmentation[k].Item1 - 1;
                int sourceEndIndex = sourceSegmentation[k].Item2;
                int targetCut = targetSegmentCuts[k];

                for (int j = trgPhraseStartIndex; j < targetCut; j++)
                {
                    builder.AppendToken(
                        targetTokens[j],
                        targetUnknownWords.Contains(j) ? TranslationSources.None : TranslationSources.Smt
                    );
                }

                int srcPhraseLen = sourceEndIndex - sourceStartIndex;
                int trgPhraseLen = targetCut - trgPhraseStartIndex;
                WordAlignmentMatrix waMatrix;
                if (srcPhraseLen == 1 && trgPhraseLen == 1)
                {
                    waMatrix = new WordAlignmentMatrix(1, 1) { [0, 0] = true };
                }
                else
                {
                    var srcPhrase = new string[srcPhraseLen];
                    for (int i = 0; i < srcPhraseLen; i++)
                        srcPhrase[i] = normalizedSourceTokens[sourceStartIndex + i];
                    var trgPhrase = new string[trgPhraseLen];
                    for (int j = 0; j < trgPhraseLen; j++)
                        trgPhrase[j] = normalizedTargetTokens[trgPhraseStartIndex + j];
                    waMatrix = _smtModel.WordAligner.Align(srcPhrase, trgPhrase);
                }
                builder.MarkPhrase(Range<int>.Create(sourceStartIndex, sourceEndIndex), waMatrix);
                trgPhraseStartIndex += trgPhraseLen;
            }

            TranslationResult result = builder.ToResult(DetokenizeTarget(targetTokens), sourceTokens);
            _confidenceEstimator.Estimate(sourceTokens, result);
            return result;
        }

        private IReadOnlyList<Tuple<int, int>> GetSourceSegmentation(IntPtr data, uint phraseCount)
        {
            int sizeOfPtr = Marshal.SizeOf<IntPtr>();
            int sizeOfUInt = Marshal.SizeOf<uint>();
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
                    sourceSegmentation[i] = Tuple.Create(
                        Marshal.ReadInt32(array, 0 * sizeOfUInt),
                        Marshal.ReadInt32(array, 1 * sizeOfUInt)
                    );
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
            int sizeOfUInt = Marshal.SizeOf<uint>();
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

        private ISet<int> GetTargetUnknownWords(IntPtr data, int targetWordCount)
        {
            int sizeOfUInt = Marshal.SizeOf<uint>();
            IntPtr nativeTargetUnknownWords = Marshal.AllocHGlobal(targetWordCount * sizeOfUInt);
            try
            {
                var targetUnknownWords = new HashSet<int>();
                uint count = Thot.tdata_getTargetUnknownWords(data, nativeTargetUnknownWords, (uint)targetWordCount);
                for (int i = 0; i < count; i++)
                    targetUnknownWords.Add(Marshal.ReadInt32(nativeTargetUnknownWords, i * sizeOfUInt));
                return targetUnknownWords;
            }
            finally
            {
                Marshal.FreeHGlobal(nativeTargetUnknownWords);
            }
        }

        private IReadOnlyList<string> NormalizeSource(IReadOnlyList<string> sourceTokens)
        {
            if (_smtModel.LowercaseSource)
                return sourceTokens.Lowercase();
            return sourceTokens;
        }

        private IReadOnlyList<string> NormalizeTarget(IReadOnlyList<string> targetTokens)
        {
            if (_smtModel.LowercaseTarget)
                return targetTokens.Lowercase();
            return targetTokens;
        }

        private IReadOnlyList<string> TokenizeSource(string sourceSegment)
        {
            return _smtModel.SourceTokenizer.Tokenize(sourceSegment).ToArray();
        }

        private IReadOnlyList<string> TokenizeTarget(string targetSegment)
        {
            return _smtModel.TargetTokenizer.Tokenize(targetSegment).ToArray();
        }

        private string DetokenizeTarget(IReadOnlyList<string> targetTokens)
        {
            return _smtModel.TargetDetokenizer.Detokenize(targetTokens);
        }

        private IReadOnlyList<string> DenormalizeTarget(IReadOnlyList<string> normalizedTargetTokens)
        {
            return _smtModel.Truecaser?.Truecase(normalizedTargetTokens) ?? normalizedTargetTokens;
        }

        protected override void DisposeUnmanagedResources()
        {
            Thot.decoder_close(_decoderHandle);
        }
    }
}
