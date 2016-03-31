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
		public static extern void decoder_close(IntPtr decoderHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr session_translate(IntPtr sessionHandle, [MarshalAs(UnmanagedType.LPWStr)] string sentence);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr session_translateInteractively(IntPtr sessionHandle, [MarshalAs(UnmanagedType.LPWStr)] string sentence);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr session_addStringToPrefix(IntPtr sessionHandle, [MarshalAs(UnmanagedType.LPWStr)] string addition);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr session_setPrefix(IntPtr sessionHandle, [MarshalAs(UnmanagedType.LPWStr)] string prefix);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void session_trainSentencePair(IntPtr sessionHandle, [MarshalAs(UnmanagedType.LPWStr)] string sourceSentence, [MarshalAs(UnmanagedType.LPWStr)] string targetSentence);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void session_close(IntPtr sessionHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr result_getTranslation(IntPtr resultHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern float result_getWordConfidence(IntPtr resultHandle, int wordIndex);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern int result_getWordCount(IntPtr resultHandle);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern void result_cleanup(IntPtr resultHandle);
	}
}
