using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SIL.Machine.Translation.Thot
{
	internal static class Thot
	{
		private const int DefaultTranslationBufferLength = 1024;

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr decoder_open(string cfgFileName);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr decoder_openSession(IntPtr decoderHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void decoder_saveModels(IntPtr decoderHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr decoder_getSingleWordAlignmentModel(IntPtr decoderHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr decoder_getInverseSingleWordAlignmentModel(IntPtr decoderHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void decoder_setLlWeights(IntPtr decoderHandle, float[] weights, uint capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void decoder_close(IntPtr decoderHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr session_translate(IntPtr sessionHandle, IntPtr sentence);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint session_translateNBest(IntPtr sessionHandle, uint n, IntPtr sentence, IntPtr[] results);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr session_translateWordGraph(IntPtr sessionHandle, IntPtr sentence);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr session_getBestPhraseAlignment(IntPtr sessionHandle, IntPtr sentence, IntPtr translation);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr session_translateInteractively(IntPtr sessionHandle, IntPtr sentence);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr session_addStringToPrefix(IntPtr sessionHandle, IntPtr addition);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr session_setPrefix(IntPtr sessionHandle, IntPtr prefix);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void session_trainSentencePair(IntPtr sessionHandle, IntPtr sourceSentence, IntPtr targetSentence, IntPtr matrix, uint iLen, uint jLen);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void session_close(IntPtr sessionHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint tdata_getTarget(IntPtr dataHandle, IntPtr target, uint capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint tdata_getPhraseCount(IntPtr dataHandle);
		
		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint tdata_getSourceSegmentation(IntPtr dataHandle, IntPtr sourceSegmentation, uint capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint tdata_getTargetSegmentCuts(IntPtr dataHandle, IntPtr targetSegmentCuts, uint capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint tdata_getTargetUnknownWords(IntPtr dataHandle, IntPtr targetUnknownWords, uint capacity);

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
		public static extern IntPtr swAlignModel_create();

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr swAlignModel_open(string prefFileName);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void swAlignModel_addSentencePair(IntPtr swAlignModelHandle, IntPtr sourceSentence, IntPtr targetSentence, IntPtr matrix, uint iLen, uint jLen);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void swAlignModel_train(IntPtr swAlignModelHandle, uint numIters);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void swAlignModel_save(IntPtr swAlignModelHandle, string prefFileName);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern float swAlignModel_getTranslationProbability(IntPtr swAlignModelHandle, IntPtr sourceWord, IntPtr targetWord);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern float swAlignModel_getBestAlignment(IntPtr swAlignModelHandle, IntPtr sourceSentence, IntPtr targetSentence, IntPtr matrix, ref uint iLen, ref uint jLen);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void swAlignModel_close(IntPtr swAlignModelHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool giza_symmetr1(string lhsFileName, string rhsFileName, string outputFileName, bool transpose);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool phraseModel_generate(string alignmentFileName, int maxPhraseLength, string tableFileName);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr langModel_open(string prefFileName);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern float langModel_getSentenceProbability(IntPtr lmHandle, IntPtr sentence);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void langModel_close(IntPtr lmHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr llWeightUpdater_create();

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void llWeightUpdater_updateClosedCorpus(IntPtr llWeightUpdaterHandle, IntPtr[] references, IntPtr nblists, IntPtr scoreComps, uint[] nblistLens,
			double[] weights, uint numSents, uint numWeights);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void llWeightUpdater_close(IntPtr llWeightUpdaterHandle);

		public static IntPtr AllocNativeMatrix(int iLen, int jLen)
		{
			int sizeOfPtr = Marshal.SizeOf(typeof(IntPtr));
			int sizeOfInt = Marshal.SizeOf(typeof(int));
			IntPtr nativeMatrix = Marshal.AllocHGlobal(iLen * sizeOfPtr);
			for (int i = 0; i < iLen; i++)
			{
				IntPtr array = Marshal.AllocHGlobal(jLen * sizeOfInt);
				for (int j = 0; j < jLen; j++)
					Marshal.WriteInt32(array, j * sizeOfInt, -1);
				Marshal.WriteIntPtr(nativeMatrix, i * sizeOfPtr, array);
			}

			return nativeMatrix;
		}

		public static IntPtr ConvertWordAlignmentMatrixToNativeMatrix(WordAlignmentMatrix matrix)
		{
			int sizeOfPtr = Marshal.SizeOf(typeof(IntPtr));
			int sizeOfInt = Marshal.SizeOf(typeof(int));
			IntPtr nativeMatrix = Marshal.AllocHGlobal(matrix.I * sizeOfPtr);
			for (int i = 0; i < matrix.I; i++)
			{
				IntPtr array = Marshal.AllocHGlobal(matrix.J * sizeOfInt);
				for (int j = 0; j < matrix.J; j++)
					Marshal.WriteInt32(array, j * sizeOfInt, (int) matrix[i, j]);
				Marshal.WriteIntPtr(nativeMatrix, i * sizeOfPtr, array);
			}
			return nativeMatrix;
		}

		public static WordAlignmentMatrix ConvertNativeMatrixToWordAlignmentMatrix(IntPtr nativeMatrix, uint iLen, uint jLen)
		{
			int sizeOfPtr = Marshal.SizeOf(typeof(IntPtr));
			int sizeOfInt = Marshal.SizeOf(typeof(int));
			var matrix = new WordAlignmentMatrix((int) iLen, (int) jLen);
			for (int i = 0; i < matrix.I; i++)
			{
				IntPtr array = Marshal.ReadIntPtr(nativeMatrix, i * sizeOfPtr);
				for (int j = 0; j < matrix.J; j++)
				{
					int intVal = Marshal.ReadInt32(array, j * sizeOfInt);
					AlignmentType value;
					if (intVal > 0)
						value = AlignmentType.Aligned;
					else if (intVal == 0)
						value = AlignmentType.NotAligned;
					else
						value = AlignmentType.Unknown;
					matrix[i, j] = value;
				}
			}

			return matrix;
		}

		public static void FreeNativeMatrix(IntPtr nativeMatrix, uint iLen)
		{
			int sizeOfPtr = Marshal.SizeOf(typeof(IntPtr));
			for (int i = 0; i < iLen; i++)
			{
				IntPtr array = Marshal.ReadIntPtr(nativeMatrix, i * sizeOfPtr);
				Marshal.FreeHGlobal(array);
			}
			Marshal.FreeHGlobal(nativeMatrix);
		}

		public static IntPtr ConvertStringsToNativeUtf8(IEnumerable<string> managedStrings)
		{
			return ConvertStringToNativeUtf8(string.Join(" ", managedStrings));
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

		public static string ConvertNativeUtf8ToString(IntPtr nativeUtf8, uint len)
		{
			var buffer = new byte[len];
			Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
			return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
		}

		public static T DoTranslate<T>(IntPtr sessionHandle, Func<IntPtr, IntPtr, IntPtr> translateFunc, IEnumerable<string> input, bool addTrailingSpace,
			IList<string> sourceSegment, Func<IList<string>, IList<string>, IntPtr, T> createResult)
		{
			IntPtr inputPtr = ConvertStringToNativeUtf8(string.Join(" ", input) + (addTrailingSpace ? " " : ""));
			IntPtr data = IntPtr.Zero;
			try
			{
				data = translateFunc(sessionHandle, inputPtr);
				return DoCreateResult(sourceSegment, data, createResult);
			}
			finally
			{
				if (data != IntPtr.Zero)
					tdata_destroy(data);
				Marshal.FreeHGlobal(inputPtr);
			}
		}

		public static IEnumerable<T> DoTranslateNBest<T>(IntPtr sessionHandle, Func<IntPtr, uint, IntPtr, IntPtr[], uint> translateFunc, int n, IEnumerable<string> input,
			bool addTrailingSpace, IList<string> sourceSegment, Func<IList<string>, IList<string>, IntPtr, T> createResult)
		{
			IntPtr inputPtr = ConvertStringToNativeUtf8(string.Join(" ", input) + (addTrailingSpace ? " " : ""));
			var results = new IntPtr[n];
			try
			{
				uint len = translateFunc(sessionHandle, (uint) n, inputPtr, results);
				return results.Take((int) len).Select(data => DoCreateResult(sourceSegment, data, createResult)).ToArray();
			}
			finally
			{
				foreach (IntPtr data in results.Where(d => d != IntPtr.Zero))
					tdata_destroy(data);
				Marshal.FreeHGlobal(inputPtr);
			}
		}

		private static T DoCreateResult<T>(IList<string> sourceSegment, IntPtr data, Func<IList<string>, IList<string>, IntPtr, T> createResult)
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
				string translation = ConvertNativeUtf8ToString(translationPtr, len);
				string[] targetSegment = translation.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
				return createResult(sourceSegment, targetSegment, data);
			}
			finally
			{
				Marshal.FreeHGlobal(translationPtr);
			}
		}

		public static void TrainSegmentPair(IntPtr sessionHandle, IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment, WordAlignmentMatrix matrix)
		{
			IntPtr nativeSourceSegment = ConvertStringsToNativeUtf8(sourceSegment);
			IntPtr nativeTargetSegment = ConvertStringsToNativeUtf8(targetSegment);
			IntPtr nativeMatrix = IntPtr.Zero;
			uint iLen = 0, jLen = 0;
			if (matrix != null)
			{
				nativeMatrix = ConvertWordAlignmentMatrixToNativeMatrix(matrix);
				iLen = (uint) matrix.I;
				jLen = (uint) matrix.J;
			}

			try
			{
				session_trainSentencePair(sessionHandle, nativeSourceSegment, nativeTargetSegment, nativeMatrix, iLen, jLen);
			}
			finally
			{
				FreeNativeMatrix(nativeMatrix, iLen);
				Marshal.FreeHGlobal(nativeTargetSegment);
				Marshal.FreeHGlobal(nativeSourceSegment);
			}
		}
	}
}
