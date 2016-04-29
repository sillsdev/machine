using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class ThotSmtSession : DisposableBase, ISmtSession
	{
		private const int DefaultTranslationBufferLength = 1024;
		private const float NullWordConfidenceThreshold = 0.03f;

		private readonly ThotSmtEngine _engine;
		private readonly IntPtr _handle;
		private readonly List<string> _sourceSegment; 
		private readonly ReadOnlyList<string> _readOnlySourceSegment;
		private readonly List<string> _prefix;
		private readonly ReadOnlyList<string> _readOnlyPrefix; 
		private bool _isLastWordPartial;
		private bool _isTranslatingInteractively;

		internal ThotSmtSession(ThotSmtEngine engine)
		{
			_engine = engine;
			_handle = Thot.decoder_openSession(_engine.Handle);
			_sourceSegment = new List<string>();
			_readOnlySourceSegment = new ReadOnlyList<string>(_sourceSegment);
			_prefix = new List<string>();
			_readOnlyPrefix = new ReadOnlyList<string>(_prefix);
			_isLastWordPartial = true;
		}

		public TranslationResult Translate(IEnumerable<string> segment)
		{
			CheckDisposed();

			string[] segmentArray = segment.ToArray();
			return CreateResult(segmentArray, DoTranslate(Thot.session_translate, segmentArray));
		}

		public IReadOnlyList<string> SourceSegment
		{
			get { return _readOnlySourceSegment; }
		}

		public IReadOnlyList<string> Prefix
		{
			get { return _readOnlyPrefix; }
		}

		public bool IsLastWordPartial
		{
			get { return _isLastWordPartial; }
		}

		public TranslationResult TranslateInteractively(IEnumerable<string> segment)
		{
			CheckDisposed();

			Reset();
			_sourceSegment.AddRange(segment);
			_isTranslatingInteractively = true;
			return CreateResult(_sourceSegment, DoTranslate(Thot.session_translateInteractively, _sourceSegment));
		}

		public TranslationResult AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			CheckDisposed();
			if (!_isTranslatingInteractively)
				throw new InvalidOperationException("An interactive translation has not been started.");

			string[] additionArray = addition.ToArray();
			_prefix.AddRange(additionArray);
			return CreateResult(_sourceSegment, DoTranslate(Thot.session_addStringToPrefix, additionArray, !isLastWordPartial));
		}

		public TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			CheckDisposed();
			if (!_isTranslatingInteractively)
				throw new InvalidOperationException("An interactive translation has not been started.");

			_prefix.Clear();
			_prefix.AddRange(prefix);
			_isLastWordPartial = isLastWordPartial;
			return CreateResult(_sourceSegment, DoTranslate(Thot.session_setPrefix, _prefix, !isLastWordPartial));
		}

		public void Reset()
		{
			_sourceSegment.Clear();
			_prefix.Clear();
			_isLastWordPartial = true;
			_isTranslatingInteractively = false;
		}

		private IList<string> DoTranslate(Func<IntPtr, IntPtr, IntPtr, int, int> translateFunc, IEnumerable<string> input,
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

		private TranslationResult CreateResult(IList<string> sourceSegment, IList<string> targetSegment)
		{
			WordAlignmentMatrix waMatrix;
			_engine.SegmentAligner.GetBestAlignment(sourceSegment, targetSegment, out waMatrix);
			var confidences = new double[targetSegment.Count];
			AlignedWordPair[,] alignment = new AlignedWordPair[waMatrix.I, waMatrix.J];
			for (int j = 0; j < waMatrix.J; j++)
			{
				string targetWord = targetSegment[j];
				double totalProb = 0;
				int alignedWordCount = 0;
				for (int i = 0; i < waMatrix.I; i++)
				{
					if (waMatrix[i, j])
					{
						string sourceWord = sourceSegment[i];
						double prob = _engine.SegmentAligner.GetTranslationProbability(sourceWord, targetWord);
						TranslationSources sources = TranslationSources.Smt;
						if (prob < NullWordConfidenceThreshold && sourceWord == targetWord)
						{
							prob = 0;
							sources = TranslationSources.None;
						}
						alignment[i, j] = new AlignedWordPair(i, j, prob, sources);
						totalProb += prob;
						alignedWordCount++;
					}
				}

				confidences[j] = alignedWordCount == 0 ? _engine.SegmentAligner.GetTranslationProbability(null, targetWord) : totalProb / alignedWordCount;
			}

			return new TranslationResult(sourceSegment, targetSegment, confidences, alignment);
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
