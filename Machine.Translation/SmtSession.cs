using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class SmtSession : DisposableBase
	{
		private readonly SmtEngine _decoder;
		private readonly IntPtr _handle;

		internal SmtSession(SmtEngine decoder)
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
			string translation = Marshal.PtrToStringUni(Thot.result_getTranslation(resultHandle));
			return new SmtResult(translation, sourceWordIndices, confidences);
		}

		public SmtResult Translate(IEnumerable<string> segment)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_translate(_handle, string.Join(" ", segment));
			SmtResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public SmtResult TranslateInteractively(IEnumerable<string> segment)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_translateInteractively(_handle, string.Join(" ", segment));
			SmtResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public SmtResult AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_addStringToPrefix(_handle, string.Join(" ", addition) + (isLastWordPartial ? "" : " "));
			SmtResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public SmtResult SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_setPrefix(_handle, string.Join(" ", prefix) + (isLastWordPartial ? "" : " "));
			SmtResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public void Train(IEnumerable<string> sourceSentence, IEnumerable<string> targetSentence)
		{
			CheckDisposed();

			Thot.session_trainSentencePair(_handle, string.Join(" ", sourceSentence), string.Join(" ", targetSentence));
		}

		protected override void DisposeUnmanagedResources()
		{
			Thot.session_close(_handle);
			_decoder.RemoveSession(this);
		}
	}
}
