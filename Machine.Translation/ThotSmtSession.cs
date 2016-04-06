using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class ThotSmtSession : DisposableBase, ISmtSession
	{
		private const int DefaultTranslationBufferLength = 1024;

		private readonly ThotSmtEngine _decoder;
		private readonly IntPtr _handle;

		internal ThotSmtSession(ThotSmtEngine decoder)
		{
			_decoder = decoder;
			_handle = Thot.decoder_openSession(_decoder.Handle);
		}

		private SmtResult CreateResult(string[] sourceWords, string translation)
		{
			var sourceWordIndices = new List<int>();
			var confidences = new List<float>();
			string[] translationWords = translation.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 0; i < translationWords.Length; i++)
			{
				float bestConfidence = 0;
				int bestIndex = 0;
				for (int j = 0; j < sourceWords.Length; j++)
				{
					if (IsPunctuation(translationWords[i]) != IsPunctuation(sourceWords[j]))
						continue;

					float confidence;
					if (IsNumber(translationWords[i]) && translationWords[i] == sourceWords[j])
					{
						confidence = 1;
					}
					else
					{
						confidence = Thot.decoder_getWordConfidence(_decoder.Handle, ConvertStringToNativeUtf8(sourceWords[j]),
							ConvertStringToNativeUtf8(translationWords[i]));
					}

					if (confidence > bestConfidence)
					{
						bestConfidence = confidence;
						bestIndex = j;
					}
					else if (Math.Abs(confidence - bestConfidence) < float.Epsilon && Math.Abs(i - j) < Math.Abs(i - bestIndex))
					{
						bestIndex = j;
					}
				}
				sourceWordIndices.Add(bestIndex);
				confidences.Add(bestConfidence);
			}
			return new SmtResult(translationWords, sourceWordIndices, confidences);
		}

		private static bool IsPunctuation(string word)
		{
			return word.All(char.IsPunctuation);
		}

		private static bool IsNumber(string word)
		{
			return word.All(char.IsNumber);
		}

		public SmtResult Translate(IEnumerable<string> segment)
		{
			CheckDisposed();

			return DoTranslate(Thot.session_translate, segment);
		}

		public SmtResult TranslateInteractively(IEnumerable<string> segment)
		{
			CheckDisposed();

			return DoTranslate(Thot.session_translateInteractively, segment);
		}

		public SmtResult AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			CheckDisposed();

			return DoTranslate(Thot.session_addStringToPrefix, addition, !isLastWordPartial);
		}

		public SmtResult SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			CheckDisposed();

			return DoTranslate(Thot.session_setPrefix, prefix, !isLastWordPartial);
		}

		private SmtResult DoTranslate(Func<IntPtr, IntPtr, IntPtr, int, int> translateFunc, IEnumerable<string> source,
			bool addTrailingSpace = false)
		{
			string[] sourceWords = source.ToArray();
			IntPtr sentencePtr = ConvertStringToNativeUtf8(string.Join(" ", sourceWords) + (addTrailingSpace ? " " : ""));
			IntPtr translationPtr = Marshal.AllocHGlobal(DefaultTranslationBufferLength);
			int len = translateFunc(_handle, sentencePtr, translationPtr, DefaultTranslationBufferLength);
			if (len > DefaultTranslationBufferLength)
			{
				translationPtr = Marshal.ReAllocHGlobal(translationPtr, (IntPtr)len);
				len = translateFunc(_handle, sentencePtr, translationPtr, len);
			}
			string translation = ConvertNativeUtf8ToString(translationPtr, len);

			Marshal.FreeHGlobal(translationPtr);
			Marshal.FreeHGlobal(sentencePtr);

			return CreateResult(sourceWords, translation);
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

		private static string ConvertNativeUtf8ToString(IntPtr nativeUtf8, int len)
		{
			var buffer = new byte[len];
			Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
			return Encoding.UTF8.GetString(buffer);
		}
	}
}
