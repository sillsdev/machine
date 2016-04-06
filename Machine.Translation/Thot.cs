using System;
using System.Runtime.InteropServices;

namespace SIL.Machine.Translation
{
	internal static class Thot
	{
		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr decoder_open(string cfgFileName);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr decoder_openSession(IntPtr decoderHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void decoder_saveModels(IntPtr decoderHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern float decoder_getWordConfidence(IntPtr decoderHandle, IntPtr sourceWord, IntPtr targetWord);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void decoder_close(IntPtr decoderHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern int session_translate(IntPtr sessionHandle, IntPtr sentence, IntPtr translation, int capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern int session_translateInteractively(IntPtr sessionHandle, IntPtr sentence, IntPtr translation, int capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern int session_addStringToPrefix(IntPtr sessionHandle, IntPtr addition, IntPtr translation, int capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern int session_setPrefix(IntPtr sessionHandle, IntPtr prefix, IntPtr translation, int capacity);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void session_trainSentencePair(IntPtr sessionHandle, IntPtr sourceSentence, IntPtr targetSentence);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void session_close(IntPtr sessionHandle);
	}
}
