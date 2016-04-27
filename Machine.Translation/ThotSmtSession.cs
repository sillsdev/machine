using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class ThotSmtSession : DisposableBase, ISmtSession
	{
		private const int DefaultTranslationBufferLength = 1024;

		private readonly ThotSmtEngine _engine;
		private readonly IntPtr _handle;

		internal ThotSmtSession(ThotSmtEngine engine)
		{
			_engine = engine;
			_handle = Thot.decoder_openSession(_engine.Handle);
		}

		public IEnumerable<string> Translate(IEnumerable<string> segment)
		{
			CheckDisposed();

			return DoTranslate(Thot.session_translate, segment);
		}

		public IEnumerable<string> TranslateInteractively(IEnumerable<string> segment)
		{
			CheckDisposed();

			return DoTranslate(Thot.session_translateInteractively, segment);
		}

		public IEnumerable<string> AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			CheckDisposed();

			return DoTranslate(Thot.session_addStringToPrefix, addition, !isLastWordPartial);
		}

		public IEnumerable<string> SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			CheckDisposed();

			return DoTranslate(Thot.session_setPrefix, prefix, !isLastWordPartial);
		}

		private IEnumerable<string> DoTranslate(Func<IntPtr, IntPtr, IntPtr, int, int> translateFunc, IEnumerable<string> input,
			bool addTrailingSpace = false)
		{
			IntPtr inputPtr = Thot.ConvertStringToNativeUtf8(string.Join(" ", input) + (addTrailingSpace ? " " : ""));
			IntPtr translationPtr = Marshal.AllocHGlobal(DefaultTranslationBufferLength);
			try
			{
				int len = translateFunc(_handle, inputPtr, translationPtr, DefaultTranslationBufferLength);
				if (len > DefaultTranslationBufferLength)
				{
					translationPtr = Marshal.ReAllocHGlobal(translationPtr, (IntPtr)len);
					len = translateFunc(_handle, inputPtr, translationPtr, len);
				}
				string translation = Thot.ConvertNativeUtf8ToString(translationPtr, len);
				return translation.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
			}
			finally
			{
				Marshal.FreeHGlobal(translationPtr);
				Marshal.FreeHGlobal(inputPtr);
			}
		}

		public void Train(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment)
		{
			CheckDisposed();

			IntPtr nativeSourceSegment = Thot.ConvertStringsToNativeUtf8(sourceSegment);
			IntPtr nativeTargetSegment = Thot.ConvertStringsToNativeUtf8(targetSegment);
			try
			{
				Thot.session_trainSentencePair(_handle, nativeSourceSegment, nativeTargetSegment);
			}
			finally
			{
				Marshal.FreeHGlobal(nativeTargetSegment);
				Marshal.FreeHGlobal(nativeSourceSegment);
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
