using System;
using System.Collections.Generic;
using SIL.Collections;

namespace SIL.Machine.Translation
{
	public class ThotSession : DisposableBase
	{
		private readonly ThotDecoder _decoder;
		private readonly IntPtr _handle;

		internal ThotSession(ThotDecoder decoder)
		{
			_decoder = decoder;
			_handle = Thot.decoder_startSession(_decoder.Handle);
		}

		private static ThotResult CreateResult(IntPtr resultHandle)
		{
			var confidences = new List<float>();
			for (int i = 0; i < Thot.result_getWordCount(resultHandle); i++)
				confidences.Add(Thot.result_getWordConfidence(resultHandle, i));
			return new ThotResult(Thot.result_getTranslation(resultHandle), confidences);
		}

		public ThotResult Translate(string sentence)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_translate(_handle, sentence);
			ThotResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public ThotResult TranslateInteractively(string sentence)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_translateInteractively(_handle, sentence);
			ThotResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public ThotResult AddStringToPrefix(string addition)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_addStringToPrefix(_handle, addition);
			ThotResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public ThotResult SetPrefix(string prefix)
		{
			CheckDisposed();

			IntPtr resultHandle = Thot.session_setPrefix(_handle, prefix);
			ThotResult result = CreateResult(resultHandle);
			Thot.result_cleanup(resultHandle);
			return result;
		}

		public void Train(string sourceSentence, string targetSentence)
		{
			CheckDisposed();

			Thot.session_trainSentencePair(_handle, sourceSentence, targetSentence);
		}

		protected override void DisposeUnmanagedResources()
		{
			Thot.decoder_endSession(_handle);
			_decoder.RemoveSession(this);
		}
	}
}
