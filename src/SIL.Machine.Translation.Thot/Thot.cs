using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SIL.Machine.Translation.Thot
{
    internal static class Thot
    {
        private const int DefaultTranslationBufferLength = 1024;

        public enum AlignmentModelType
        {
            Ibm1 = 0,
            Ibm2 = 1,
            Hmm = 2,
            Ibm3 = 3,
            Ibm4 = 4,
            FastAlign = 5,
            IncrIbm1 = 6,
            IncrIbm2 = 7,
            IncrHmm = 8
        }

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr smtModel_create(AlignmentModelType alignmentModelType);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool smtModel_loadTranslationModel(IntPtr smtModelHandle, string tmFileNamePrefix);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool smtModel_loadLanguageModel(IntPtr smtModelHandle, string lmFileName);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void smtModel_setNonMonotonicity(IntPtr smtModelHandle, uint nomon);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void smtModel_setW(IntPtr smtModelHandle, float w);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void smtModel_setA(IntPtr smtModelHandle, uint a);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void smtModel_setE(IntPtr smtModelHandle, uint e);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void smtModel_setHeuristic(IntPtr smtModelHandle, uint heuristic);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void smtModel_setOnlineTrainingParameters(
            IntPtr smtModelHandle,
            uint algorithm,
            uint learningRatePolicy,
            float learnStepSize,
            uint emIters,
            uint e,
            uint r
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void smtModel_setWeights(IntPtr smtModelHandle, float[] weights, uint capacity);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr smtModel_getSingleWordAlignmentModel(IntPtr smtModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr smtModel_getInverseSingleWordAlignmentModel(IntPtr smtModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool smtModel_saveModels(IntPtr smtModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void smtModel_close(IntPtr smtModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr decoder_create(IntPtr smtModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void decoder_setS(IntPtr decoderHandle, uint s);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void decoder_setBreadthFirst(IntPtr decoderHandle, bool breadthFirst);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void decoder_setG(IntPtr decoderHandle, uint g);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr decoder_translate(IntPtr decoderHandle, IntPtr sentence);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint decoder_translateNBest(
            IntPtr decoderHandle,
            uint n,
            IntPtr sentence,
            IntPtr[] results
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr decoder_getWordGraph(IntPtr decoderHandle, IntPtr sentence);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr decoder_getBestPhraseAlignment(
            IntPtr decoderHandle,
            IntPtr sentence,
            IntPtr translation
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool decoder_trainSentencePair(
            IntPtr decoderHandle,
            IntPtr sourceSentence,
            IntPtr targetSentence
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void decoder_close(IntPtr decoderHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tdata_getTarget(IntPtr dataHandle, IntPtr target, uint capacity);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tdata_getPhraseCount(IntPtr dataHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tdata_getSourceSegmentation(
            IntPtr dataHandle,
            IntPtr sourceSegmentation,
            uint capacity
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tdata_getTargetSegmentCuts(
            IntPtr dataHandle,
            IntPtr targetSegmentCuts,
            uint capacity
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tdata_getTargetUnknownWords(
            IntPtr dataHandle,
            IntPtr targetUnknownWords,
            uint capacity
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double tdata_getScore(IntPtr dataHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tdata_getScoreComponents(IntPtr dataHandle, double[] scoreComps, uint capacity);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void tdata_destroy(IntPtr dataHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint wg_getString(IntPtr wgHandle, IntPtr wordGraphStr, uint capacity);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double wg_getInitialStateScore(IntPtr wgHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void wg_destroy(IntPtr wgHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr swAlignModel_create(AlignmentModelType type, IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr swAlignModel_open(AlignmentModelType type, string prefFileName);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint swAlignModel_getMaxSentenceLength(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_setVariationalBayes(IntPtr swAlignModelHandle, bool variationalBayes);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool swAlignModel_getVariationalBayes(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_setFastAlignP0(IntPtr swAlignModelHandle, double p0);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double swAlignModel_getFastAlignP0(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_setHmmP0(IntPtr swAlignModelHandle, double p0);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double swAlignModel_getHmmP0(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_setHmmLexicalSmoothingFactor(
            IntPtr swAlignModelHandle,
            double lexicalSmoothingFactor
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double swAlignModel_getHmmLexicalSmoothingFactor(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_setHmmAlignmentSmoothingFactor(
            IntPtr swAlignModelHandle,
            double alignmentSmoothingFactor
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double swAlignModel_getHmmAlignmentSmoothingFactor(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_setIbm2CompactAlignmentTable(
            IntPtr swAlignModelHandle,
            bool compactAlignmentTable
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool swAlignModel_getIbm2CompactAlignmentTable(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_setIbm3FertilitySmoothingFactor(
            IntPtr swAlignModelHandle,
            double fertilitySmoothingFactor
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double swAlignModel_getIbm3FertilitySmoothingFactor(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_setIbm3CountThreshold(IntPtr swAlignModelHandle, double countThreshold);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double swAlignModel_getIbm3CountThreshold(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_setIbm4DistortionSmoothingFactor(
            IntPtr swAlignModelHandle,
            double distortionSmoothingFactor
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double swAlignModel_getIbm4DistortionSmoothingFactor(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint swAlignModel_getSourceWordCount(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint swAlignModel_getSourceWord(
            IntPtr swAlignModelHandle,
            uint index,
            IntPtr wordStr,
            uint capacity
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint swAlignModel_getSourceWordIndex(IntPtr swAlignModelHandle, IntPtr word);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint swAlignModel_getTargetWordCount(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint swAlignModel_getTargetWord(
            IntPtr swAlignModelHandle,
            uint index,
            IntPtr wordStr,
            uint capacity
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint swAlignModel_getTargetWordIndex(IntPtr swAlignModelHandle, IntPtr word);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_addSentencePair(
            IntPtr swAlignModelHandle,
            IntPtr sourceSentence,
            IntPtr targetSentence
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_readSentencePairs(
            IntPtr swAlignModelHandle,
            string srcFileName,
            string trgFileName,
            string countsFileName
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_mapSourceWordToWordClass(
            IntPtr swAlignModelHandle,
            IntPtr word,
            IntPtr wordClass
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_mapTargetWordToWordClass(
            IntPtr swAlignModelHandle,
            IntPtr word,
            IntPtr wordClass
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint swAlignModel_startTraining(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_train(IntPtr swAlignModelHandle, uint numIters);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_endTraining(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_clearTempVars(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_save(IntPtr swAlignModelHandle, string prefFileName);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double swAlignModel_getTranslationProbability(
            IntPtr swAlignModelHandle,
            IntPtr sourceWord,
            IntPtr targetWord
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double swAlignModel_getTranslationProbabilityByIndex(
            IntPtr swAlignModelHandle,
            uint sourceWordIndex,
            uint targetWordIndex
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double swAlignModel_getIbm2AlignmentProbability(
            IntPtr swAlignModelHandle,
            uint j,
            uint sLen,
            uint tLen,
            uint i
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double swAlignModel_getHmmAlignmentProbability(
            IntPtr swAlignModelHandle,
            uint prevI,
            uint sLen,
            uint i
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double swAlignModel_getBestAlignment(
            IntPtr swAlignModelHandle,
            IntPtr sourceSentence,
            IntPtr targetSentence,
            IntPtr matrix,
            ref uint iLen,
            ref uint jLen
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr swAlignModel_getTranslations(
            IntPtr swAlignModelHandle,
            IntPtr sourceWord,
            double threshold
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr swAlignModel_getTranslationsByIndex(
            IntPtr swAlignModelHandle,
            uint sourceWordIndex,
            double threshold
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignModel_close(IntPtr swAlignModelHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint swAlignTrans_getCount(IntPtr swAlignTransHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint swAlignTrans_getTranslations(
            IntPtr swAlignTransHandle,
            uint[] wordIndices,
            double[] probs,
            uint capacity
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void swAlignTrans_destroy(IntPtr swAlignTransHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool giza_symmetr1(
            string lhsFileName,
            string rhsFileName,
            string outputFileName,
            bool transpose
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool phraseModel_generate(
            string alignmentFileName,
            int maxPhraseLength,
            string tableFileName,
            int n
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr langModel_open(string prefFileName);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern double langModel_getSentenceProbability(IntPtr lmHandle, IntPtr sentence);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void langModel_close(IntPtr lmHandle);

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr llWeightUpdater_create();

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void llWeightUpdater_updateClosedCorpus(
            IntPtr llWeightUpdaterHandle,
            IntPtr[] references,
            IntPtr nblists,
            IntPtr scoreComps,
            uint[] nblistLens,
            float[] weights,
            uint numSents,
            uint numWeights
        );

        [DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
        public static extern void llWeightUpdater_close(IntPtr llWeightUpdaterHandle);

        public static IntPtr AllocNativeMatrix(int iLen, int jLen)
        {
            int sizeOfPtr = Marshal.SizeOf<IntPtr>();
            IntPtr nativeMatrix = Marshal.AllocHGlobal(iLen * sizeOfPtr);
            for (int i = 0; i < iLen; i++)
            {
                IntPtr array = Marshal.AllocHGlobal(jLen);
                for (int j = 0; j < jLen; j++)
                    Marshal.WriteByte(array, j, Convert.ToByte(false));
                Marshal.WriteIntPtr(nativeMatrix, i * sizeOfPtr, array);
            }

            return nativeMatrix;
        }

        public static IntPtr ConvertWordAlignmentMatrixToNativeMatrix(WordAlignmentMatrix matrix)
        {
            int sizeOfPtr = Marshal.SizeOf<IntPtr>();
            IntPtr nativeMatrix = Marshal.AllocHGlobal(matrix.RowCount * sizeOfPtr);
            for (int i = 0; i < matrix.RowCount; i++)
            {
                IntPtr array = Marshal.AllocHGlobal(matrix.ColumnCount);
                for (int j = 0; j < matrix.ColumnCount; j++)
                    Marshal.WriteByte(array, j, Convert.ToByte(matrix[i, j]));
                Marshal.WriteIntPtr(nativeMatrix, i * sizeOfPtr, array);
            }
            return nativeMatrix;
        }

        public static WordAlignmentMatrix ConvertNativeMatrixToWordAlignmentMatrix(
            IntPtr nativeMatrix,
            uint iLen,
            uint jLen
        )
        {
            int sizeOfPtr = Marshal.SizeOf<IntPtr>();
            var matrix = new WordAlignmentMatrix((int)iLen, (int)jLen);
            for (int i = 0; i < matrix.RowCount; i++)
            {
                IntPtr array = Marshal.ReadIntPtr(nativeMatrix, i * sizeOfPtr);
                for (int j = 0; j < matrix.ColumnCount; j++)
                    matrix[i, j] = Convert.ToBoolean(Marshal.ReadByte(array, j));
            }

            return matrix;
        }

        public static void FreeNativeMatrix(IntPtr nativeMatrix, uint iLen)
        {
            int sizeOfPtr = Marshal.SizeOf<IntPtr>();
            for (int i = 0; i < iLen; i++)
            {
                IntPtr array = Marshal.ReadIntPtr(nativeMatrix, i * sizeOfPtr);
                Marshal.FreeHGlobal(array);
            }
            Marshal.FreeHGlobal(nativeMatrix);
        }

        public static IntPtr ConvertSegmentToNativeUtf8(IReadOnlyList<string> segment)
        {
            return ConvertStringToNativeUtf8(string.Join(" ", segment.Select(EscapeToken)));
        }

        public static IntPtr ConvertTokenToNativeUtf8(string token)
        {
            return ConvertStringToNativeUtf8(EscapeToken(token));
        }

        public static IntPtr ConvertStringToNativeUtf8(string managedString)
        {
            int len = Encoding.UTF8.GetByteCount(managedString);
            byte[] buffer = new byte[len + 1];
            Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);
            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
            return nativeUtf8;
        }

        public static IReadOnlyList<string> ConvertNativeUtf8ToSegment(IntPtr nativeUtf8, uint len)
        {
            string segmentStr = ConvertNativeUtf8ToString(nativeUtf8, len);
            string[] segment = segmentStr.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segment.Length; i++)
                segment[i] = UnescapeToken(segment[i]);
            return segment;
        }

        public static string ConvertNativeUtf8ToToken(IntPtr nativeUtf8, uint len)
        {
            return UnescapeToken(ConvertNativeUtf8ToString(nativeUtf8, len));
        }

        public static string ConvertNativeUtf8ToString(IntPtr nativeUtf8, uint len)
        {
            byte[] buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }

        public static T DoTranslate<T>(
            IntPtr decoderHandle,
            Func<IntPtr, IntPtr, IntPtr> translateFunc,
            IReadOnlyList<string> sourceSegment,
            Func<IReadOnlyList<string>, IntPtr, T> createResult
        )
        {
            IntPtr inputPtr = ConvertSegmentToNativeUtf8(sourceSegment);
            IntPtr data = IntPtr.Zero;
            try
            {
                data = translateFunc(decoderHandle, inputPtr);
                return DoCreateResult(data, createResult);
            }
            finally
            {
                if (data != IntPtr.Zero)
                    tdata_destroy(data);
                Marshal.FreeHGlobal(inputPtr);
            }
        }

        public static T[] DoTranslateNBest<T>(
            IntPtr decoderHandle,
            Func<IntPtr, uint, IntPtr, IntPtr[], uint> translateFunc,
            int n,
            IReadOnlyList<string> sourceSegment,
            Func<IReadOnlyList<string>, IntPtr, T> createResult
        )
        {
            IntPtr inputPtr = ConvertSegmentToNativeUtf8(sourceSegment);
            var results = new IntPtr[n];
            try
            {
                uint len = translateFunc(decoderHandle, (uint)n, inputPtr, results);
                return results.Take((int)len).Select(data => DoCreateResult(data, createResult)).ToArray();
            }
            finally
            {
                foreach (IntPtr data in results.Where(d => d != IntPtr.Zero))
                    tdata_destroy(data);
                Marshal.FreeHGlobal(inputPtr);
            }
        }

        private static T DoCreateResult<T>(IntPtr data, Func<IReadOnlyList<string>, IntPtr, T> createResult)
        {
            IntPtr translationPtr = Marshal.AllocHGlobal(DefaultTranslationBufferLength);
            try
            {
                uint len = tdata_getTarget(data, translationPtr, DefaultTranslationBufferLength);
                if (len > DefaultTranslationBufferLength)
                {
                    translationPtr = Marshal.ReAllocHGlobal(translationPtr, (IntPtr)len);
                    len = tdata_getTarget(data, translationPtr, len);
                }
                IReadOnlyList<string> targetSegment = ConvertNativeUtf8ToSegment(translationPtr, len);
                return createResult(targetSegment, data);
            }
            finally
            {
                Marshal.FreeHGlobal(translationPtr);
            }
        }

        public static void TrainSegmentPair(
            IntPtr decoderHandle,
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment
        )
        {
            IntPtr nativeSourceSegment = ConvertSegmentToNativeUtf8(sourceSegment);
            IntPtr nativeTargetSegment = ConvertSegmentToNativeUtf8(targetSegment);
            try
            {
                decoder_trainSentencePair(decoderHandle, nativeSourceSegment, nativeTargetSegment);
            }
            finally
            {
                Marshal.FreeHGlobal(nativeTargetSegment);
                Marshal.FreeHGlobal(nativeSourceSegment);
            }
        }

        public static IntPtr LoadSmtModel(ThotWordAlignmentModelType alignmentModelType, ThotSmtParameters parameters)
        {
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = smtModel_create(GetAlignmentModelType(alignmentModelType, incremental: true));
                if (!smtModel_loadTranslationModel(handle, parameters.TranslationModelFileNamePrefix))
                    throw new InvalidOperationException("Unable to load translation model.");
                if (!smtModel_loadLanguageModel(handle, parameters.LanguageModelFileNamePrefix))
                    throw new InvalidOperationException("Unable to load language model.");
                smtModel_setNonMonotonicity(handle, parameters.ModelNonMonotonicity);
                smtModel_setW(handle, parameters.ModelW);
                smtModel_setA(handle, parameters.ModelA);
                smtModel_setE(handle, parameters.ModelE);
                smtModel_setHeuristic(handle, (uint)parameters.ModelHeuristic);
                smtModel_setOnlineTrainingParameters(
                    handle,
                    (uint)parameters.LearningAlgorithm,
                    (uint)parameters.LearningRatePolicy,
                    parameters.LearningStepSize,
                    parameters.LearningEMIters,
                    parameters.LearningE,
                    parameters.LearningR
                );
                if (parameters.ModelWeights != null)
                    smtModel_setWeights(handle, parameters.ModelWeights.ToArray(), (uint)parameters.ModelWeights.Count);
                return handle;
            }
            catch
            {
                if (handle != IntPtr.Zero)
                    smtModel_close(handle);
                throw;
            }
        }

        public static IntPtr LoadDecoder(IntPtr smtModelHandle, ThotSmtParameters parameters)
        {
            IntPtr handle = decoder_create(smtModelHandle);
            decoder_setS(handle, parameters.DecoderS);
            decoder_setBreadthFirst(handle, parameters.DecoderBreadthFirst);
            decoder_setG(handle, parameters.DecoderG);
            return handle;
        }

        public static IntPtr CreateAlignmentModel(ThotWordAlignmentModelType type, IntPtr modelHandle = default)
        {
            return swAlignModel_create(GetAlignmentModelType(type, incremental: false), modelHandle);
        }

        public static IntPtr OpenAlignmentModel(ThotWordAlignmentModelType type, string prefFileName)
        {
            IntPtr handle = swAlignModel_open(GetAlignmentModelType(type, incremental: false), prefFileName);
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("Unable to load word alignment model.");
            return handle;
        }

        public static AlignmentModelType GetAlignmentModelType(ThotWordAlignmentModelType type, bool incremental)
        {
            switch (type)
            {
                case ThotWordAlignmentModelType.Ibm1:
                    return incremental ? AlignmentModelType.IncrIbm1 : AlignmentModelType.Ibm1;
                case ThotWordAlignmentModelType.Ibm2:
                    return incremental ? AlignmentModelType.IncrIbm2 : AlignmentModelType.Ibm2;
                case ThotWordAlignmentModelType.Hmm:
                    return incremental ? AlignmentModelType.IncrHmm : AlignmentModelType.Hmm;
                case ThotWordAlignmentModelType.FastAlign:
                    return AlignmentModelType.FastAlign;
                case ThotWordAlignmentModelType.Ibm3:
                    return AlignmentModelType.Ibm3;
                case ThotWordAlignmentModelType.Ibm4:
                    return AlignmentModelType.Ibm4;
            }
            throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(ThotWordAlignmentModelType));
        }

        public static string EscapeToken(string token)
        {
            if (token == "|||")
                return "<3bars>";
            return token;
        }

        public static string UnescapeToken(string token)
        {
            if (token == "<3bars>")
                return "|||";
            return token;
        }
    }
}
