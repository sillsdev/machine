using System;
using System.Collections.Generic;
using SIL.Collections;

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
			var confidences = new List<float>();
			for (int i = 0; i < Thot.result_getWordCount(resultHandle); i++)
				confidences.Add(Thot.result_getWordConfidence(resultHandle, i));
			return new SmtResult(Thot.result_getTranslation(resultHandle), confidences);
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

		public SmtResult AddStringToPrefix(IEnumerable<string> addition)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_addStringToPrefix(_handle, string.Join(" ", addition) + " ");
			SmtResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public SmtResult SetPrefix(IEnumerable<string> prefix)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_setPrefix(_handle, string.Join(" ", prefix) + " ");
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
