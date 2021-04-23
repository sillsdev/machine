using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SIL.Machine.Translation.Thot
{
	internal static class Thot
	{
		public const string HmmWordAlignmentClassName = "IncrHmmP0AligModel";
		public const string Ibm1WordAlignmentClassName = "IncrIbm1AligModel";
		public const string Ibm2WordAlignmentClassName = "IncrIbm2AligModel";
		public const string SmoothedIbm1WordAlignmentClassName = "SmoothedIncrIbm1AligModel";
		public const string SmoothedIbm2WordAlignmentClassName = "SmoothedIncrIbm2AligModel";
		public const string FastAlignWordAlignmentClassName = "FastAlignModel";

		private const int DefaultTranslationBufferLength = 1024;

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr smtModel_create(string swAlignClassName);

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
		public static extern void smtModel_setOnlineTrainingParameters(IntPtr smtModelHandle, uint algorithm,
			uint learningRatePolicy, float learnStepSize, uint emIters, uint e, uint r);

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
		public static extern uint decoder_translateNBest(IntPtr decoderHandle, uint n, IntPtr sentence,
			IntPtr[] results);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr decoder_getWordGraph(IntPtr decoderHandle, IntPtr sentence);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr decoder_getBestPhraseAlignment(IntPtr decoderHandle, IntPtr sentence,
			IntPtr translation);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool decoder_trainSentencePair(IntPtr decoderHandle, IntPtr sourceSentence,
			IntPtr targetSentence);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void decoder_close(IntPtr decoderHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint tdata_getTarget(IntPtr dataHandle, IntPtr target, uint capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint tdata_getPhraseCount(IntPtr dataHandle);
		
		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint tdata_getSourceSegmentation(IntPtr dataHandle, IntPtr sourceSegmentation,
			uint capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint tdata_getTargetSegmentCuts(IntPtr dataHandle, IntPtr targetSegmentCuts,
			uint capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint tdata_getTargetUnknownWords(IntPtr dataHandle, IntPtr targetUnknownWords,
			uint capacity);

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
		public static extern IntPtr swAlignModel_create(string className);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr swAlignModel_open(string className, string prefFileName);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint swAlignModel_getSourceWordCount(IntPtr swAlignModelHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint swAlignModel_getSourceWord(IntPtr swAlignModelHandle, uint index, IntPtr wordStr,
			uint capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint swAlignModel_getTargetWordCount(IntPtr swAlignModelHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint swAlignModel_getTargetWord(IntPtr swAlignModelHandle, uint index, IntPtr wordStr,
			uint capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void swAlignModel_addSentencePair(IntPtr swAlignModelHandle, IntPtr sourceSentence,
			IntPtr targetSentence);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void swAlignModel_train(IntPtr swAlignModelHandle, uint numIters);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void swAlignModel_clearTempVars(IntPtr swAlignModelHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void swAlignModel_save(IntPtr swAlignModelHandle, string prefFileName);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern float swAlignModel_getTranslationProbability(IntPtr swAlignModelHandle, IntPtr sourceWord,
			IntPtr targetWord);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern float swAlignModel_getTranslationProbabilityByIndex(IntPtr swAlignModelHandle,
			uint sourceWordIndex, uint targetWordIndex);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern float swAlignModel_getIbm2AlignmentProbability(IntPtr swAlignModelHandle, uint j,
			uint sLen, uint tLen, uint i);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern float swAlignModel_getHmmAlignmentProbability(IntPtr swAlignModelHandle, uint prevI,
			uint sLen, uint i);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern float swAlignModel_getBestAlignment(IntPtr swAlignModelHandle, IntPtr sourceSentence,
			IntPtr targetSentence, IntPtr matrix, ref uint iLen, ref uint jLen);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr swAlignModel_getTranslations(IntPtr swAlignModelHandle, IntPtr sourceWord,
			float threshold);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr swAlignModel_getTranslationsByIndex(IntPtr swAlignModelHandle, uint sourceWordIndex,
			float threshold);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void swAlignModel_close(IntPtr swAlignModelHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint swAlignTrans_getCount(IntPtr swAlignTransHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint swAlignTrans_getTranslations(IntPtr swAlignTransHandle, uint[] wordIndices,
			float[] probs, uint capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void swAlignTrans_destroy(IntPtr swAlignTransHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool giza_symmetr1(string lhsFileName, string rhsFileName, string outputFileName,
			bool transpose);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool phraseModel_generate(string alignmentFileName, int maxPhraseLength,
			string tableFileName, int n);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr langModel_open(string prefFileName);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern float langModel_getSentenceProbability(IntPtr lmHandle, IntPtr sentence);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void langModel_close(IntPtr lmHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr llWeightUpdater_create();

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void llWeightUpdater_updateClosedCorpus(IntPtr llWeightUpdaterHandle, IntPtr[] references,
			IntPtr nblists, IntPtr scoreComps, uint[] nblistLens, float[] weights, uint numSents, uint numWeights);

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

		public static WordAlignmentMatrix ConvertNativeMatrixToWordAlignmentMatrix(IntPtr nativeMatrix, uint iLen,
			uint jLen)
		{
			int sizeOfPtr = Marshal.SizeOf<IntPtr>();
			var matrix = new WordAlignmentMatrix((int) iLen, (int) jLen);
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
			var buffer = new byte[len + 1];
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
			var buffer = new byte[len];
			Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
			return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
		}

		public static T DoTranslate<T>(IntPtr decoderHandle, Func<IntPtr, IntPtr, IntPtr> translateFunc,
			IReadOnlyList<string> sourceSegment,
			Func<IReadOnlyList<string>, IReadOnlyList<string>, IntPtr, T> createResult)
		{
			IntPtr inputPtr = ConvertSegmentToNativeUtf8(sourceSegment);
			IntPtr data = IntPtr.Zero;
			try
			{
				data = translateFunc(decoderHandle, inputPtr);
				return DoCreateResult(sourceSegment, data, createResult);
			}
			finally
			{
				if (data != IntPtr.Zero)
					tdata_destroy(data);
				Marshal.FreeHGlobal(inputPtr);
			}
		}

		public static IEnumerable<T> DoTranslateNBest<T>(IntPtr decoderHandle,
			Func<IntPtr, uint, IntPtr, IntPtr[], uint> translateFunc, int n, IReadOnlyList<string> sourceSegment,
			Func<IReadOnlyList<string>, IReadOnlyList<string>, IntPtr, T> createResult)
		{
			IntPtr inputPtr = ConvertSegmentToNativeUtf8(sourceSegment);
			var results = new IntPtr[n];
			try
			{
				uint len = translateFunc(decoderHandle, (uint) n, inputPtr, results);
				return results.Take((int) len).Select(data => DoCreateResult(sourceSegment, data, createResult))
					.ToArray();
			}
			finally
			{
				foreach (IntPtr data in results.Where(d => d != IntPtr.Zero))
					tdata_destroy(data);
				Marshal.FreeHGlobal(inputPtr);
			}
		}

		private static T DoCreateResult<T>(IReadOnlyList<string> sourceSegment, IntPtr data,
			Func<IReadOnlyList<string>, IReadOnlyList<string>, IntPtr, T> createResult)
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
				return createResult(sourceSegment, targetSegment, data);
			}
			finally
			{
				Marshal.FreeHGlobal(translationPtr);
			}
		}

		public static void TrainSegmentPair(IntPtr decoderHandle, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
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

		public static IntPtr LoadSmtModel(string swAlignClassName, ThotSmtParameters parameters)
		{
			IntPtr handle = smtModel_create(swAlignClassName);
			smtModel_loadTranslationModel(handle, parameters.TranslationModelFileNamePrefix);
			smtModel_loadLanguageModel(handle, parameters.LanguageModelFileNamePrefix);
			smtModel_setNonMonotonicity(handle, parameters.ModelNonMonotonicity);
			smtModel_setW(handle, parameters.ModelW);
			smtModel_setA(handle, parameters.ModelA);
			smtModel_setE(handle, parameters.ModelE);
			smtModel_setHeuristic(handle, (uint) parameters.ModelHeuristic);
			smtModel_setOnlineTrainingParameters(handle, (uint) parameters.LearningAlgorithm,
				(uint) parameters.LearningRatePolicy, parameters.LearningStepSize, parameters.LearningEMIters,
				parameters.LearningE, parameters.LearningR);
			if (parameters.ModelWeights != null)
				smtModel_setWeights(handle, parameters.ModelWeights.ToArray(), (uint) parameters.ModelWeights.Count);
			return handle;
		}

		public static IntPtr LoadDecoder(IntPtr smtModelHandle, ThotSmtParameters parameters)
		{
			IntPtr handle = decoder_create(smtModelHandle);
			decoder_setS(handle, parameters.DecoderS);
			decoder_setBreadthFirst(handle, parameters.DecoderBreadthFirst);
			decoder_setG(handle, parameters.DecoderG);
			return handle;
		}

		public static string GetWordAlignmentClassName<TAlignModel>()
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			string swAlignClassName = null;
			Type alignModelType = typeof(TAlignModel);
			if (alignModelType == typeof(HmmWordAlignmentModel))
				swAlignClassName = HmmWordAlignmentClassName;
			else if (alignModelType == typeof(Ibm1WordAlignmentModel))
				swAlignClassName = SmoothedIbm1WordAlignmentClassName;
			else if (alignModelType == typeof(Ibm2WordAlignmentModel))
				swAlignClassName = SmoothedIbm2WordAlignmentClassName;
			else if (alignModelType == typeof(FastAlignWordAlignmentModel))
				swAlignClassName = FastAlignWordAlignmentClassName;
			Debug.Assert(swAlignClassName != null);
			return swAlignClassName;
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
