using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class ThotSmtSession : DisposableBase, ISmtSession
	{
		private readonly ThotSmtEngine _decoder;
		private readonly IntPtr _handle;

		internal ThotSmtSession(ThotSmtEngine decoder)
		{
			_decoder = decoder;
			_handle = Thot.decoder_openSession(_decoder.Handle);
		}

		private static SmtResult CreateResult(IntPtr resultHandle)
		{
			var sourceWordIndices = new List<int>();
			var confidences = new List<float>();
			for (int i = 0; i < Thot.result_getWordCount(resultHandle); i++)
			{
				sourceWordIndices.Add(Thot.result_getAlignedSourceWordIndex(resultHandle, i));
				confidences.Add(Thot.result_getWordConfidence(resultHandle, i));
			}
			string translation = ConvertNativeUtf8ToString(Thot.result_getTranslation(resultHandle));
			return new SmtResult(translation, sourceWordIndices, confidences);
		}

		public SmtResult Translate(IEnumerable<string> segment)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_translate(_handle, ConvertStringToNativeUtf8(string.Join(" ", segment)));
			SmtResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public SmtResult TranslateInteractively(IEnumerable<string> segment)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_translateInteractively(_handle, ConvertStringToNativeUtf8(string.Join(" ", segment)));
			SmtResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public SmtResult AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_addStringToPrefix(_handle, ConvertStringToNativeUtf8(string.Join(" ", addition) + (isLastWordPartial ? "" : " ")));
			SmtResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public SmtResult SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_setPrefix(_handle, ConvertStringToNativeUtf8(string.Join(" ", prefix) + (isLastWordPartial ? "" : " ")));
			SmtResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public void Train(IEnumerable<string> sourceSentence, IEnumerable<string> targetSentence)
		{
			CheckDisposed();

			Thot.session_trainSentencePair(_handle, ConvertStringToNativeUtf8(string.Join(" ", sourceSentence)), ConvertStringToNativeUtf8(string.Join(" ", targetSentence)));
		}

		protected override void DisposeManagedResources()
		{
			_decoder.RemoveSession(this);
		}

		protected override void DisposeUnmanagedResources()
		{
			Thot.session_close(_handle);
		}

		private static IntPtr ConvertStringToNativeUtf8(string managedString)
		{
			int len = Encoding.UTF8.GetByteCount(managedString);
			var buffer = new byte[len + 1];
			Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);
			IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
			Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
			return nativeUtf8;
		}

		private static string ConvertNativeUtf8ToString(IntPtr nativeUtf8)
		{
			int len = 0;
			while (Marshal.ReadByte(nativeUtf8, len) != 0)
				len++;
			var buffer = new byte[len];
			Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
			return Encoding.UTF8.GetString(buffer);
		}
	}
}
