using System;
using System.Runtime.InteropServices;
using System.Text;

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
		public static extern float decoder_getTranslationProbability(IntPtr decoderHandle, IntPtr sourceWord, IntPtr targetWord);

		[DllImport("thot", CallingConvention = CallingConvention.Cdecl)]
		public static extern int decoder_getBestAlignment(IntPtr decoderHandle, IntPtr sourceSentence, IntPtr targetSentence, int[] alignment, int capacity);

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

		public static IntPtr ConvertStringToNativeUtf8(string managedString)
		{
			int len = Encoding.UTF8.GetByteCount(managedString);
			var buffer = new byte[len + 1];
			Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);
			IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
			Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
			return nativeUtf8;
		}

		public static string ConvertNativeUtf8ToString(IntPtr nativeUtf8, int len)
		{
			var buffer = new byte[len];
			Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
			return Encoding.UTF8.GetString(buffer);
		}
	}
}
