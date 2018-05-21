using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotWordAlignmentModel : DisposableBase, IHmmWordAlignmentModel
	{
		private readonly ThotWordVocabulary _sourceWords;
		private readonly ThotWordVocabulary _targetWords;
		private readonly bool _closeOnDispose;
		private readonly string _prefFileName;

		public ThotWordAlignmentModel()
		{
			Handle = Thot.swAlignModel_create();
			_sourceWords = new ThotWordVocabulary(Handle, true);
			_targetWords = new ThotWordVocabulary(Handle, false);
			_closeOnDispose = true;
		}

		internal ThotWordAlignmentModel(IntPtr handle)
		{
			Handle = handle;
			_sourceWords = new ThotWordVocabulary(Handle, true);
			_targetWords = new ThotWordVocabulary(Handle, false);
			_closeOnDispose = false;
		}

		public ThotWordAlignmentModel(string prefFileName, bool createNew = false)
		{
			if (!createNew && !File.Exists(prefFileName + ".src"))
				throw new FileNotFoundException("The word alignment model configuration could not be found.");

			_prefFileName = prefFileName;
			Handle = createNew || !File.Exists(prefFileName + ".src")
				? Thot.swAlignModel_create()
				: Thot.swAlignModel_open(_prefFileName);
			_sourceWords = new ThotWordVocabulary(Handle, true);
			_targetWords = new ThotWordVocabulary(Handle, false);
			_closeOnDispose = true;
		}

		internal IntPtr Handle { get; set; }

		public int TrainingIterationCount { get; set; } = 5;

		public IReadOnlyList<string> SourceWords
		{
			get
			{
				CheckDisposed();

				return _sourceWords;
			}
		}

		public IReadOnlyList<string> TargetWords
		{
			get
			{
				CheckDisposed();

				return _targetWords;
			}
		}

		public void AddSegmentPair(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			IntPtr nativeSourceSegment = Thot.ConvertStringsToNativeUtf8(sourceSegment);
			IntPtr nativeTargetSegment = Thot.ConvertStringsToNativeUtf8(targetSegment);
			try
			{
				Thot.swAlignModel_addSentencePair(Handle, nativeSourceSegment, nativeTargetSegment);
			}
			finally
			{
				Marshal.FreeHGlobal(nativeTargetSegment);
				Marshal.FreeHGlobal(nativeSourceSegment);
			}
		}

		public void Train(IProgress<ProgressStatus> progress = null)
		{
			CheckDisposed();

			for (int i = 0; i < TrainingIterationCount; i++)
			{
				progress?.Report(new ProgressStatus(i, TrainingIterationCount));
				TrainingIteration();
			}
			progress?.Report(new ProgressStatus(1.0));
		}

		public void TrainingIteration()
		{
			CheckDisposed();

			Thot.swAlignModel_train(Handle, 1);
		}

		public void Save()
		{
			CheckDisposed();

			if (string.IsNullOrEmpty(_prefFileName))
				throw new InvalidOperationException("This word alignment model cannot be saved.");
			Thot.swAlignModel_save(Handle, _prefFileName);
		}

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			CheckDisposed();

			IntPtr nativeSourceWord = Thot.ConvertStringToNativeUtf8(sourceWord ?? "NULL");
			IntPtr nativeTargetWord = Thot.ConvertStringToNativeUtf8(targetWord ?? "NULL");
			try
			{
				return Thot.swAlignModel_getTranslationProbability(Handle, nativeSourceWord, nativeTargetWord);
			}
			finally
			{
				Marshal.FreeHGlobal(nativeTargetWord);
				Marshal.FreeHGlobal(nativeSourceWord);
			}
		}

		public double GetTranslationProbability(int sourceWordIndex, int targetWordIndex)
		{
			CheckDisposed();

			return Thot.swAlignModel_getTranslationProbabilityByIndex(Handle, (uint) sourceWordIndex,
				(uint) targetWordIndex);
		}

		/// <summary>
		/// Gets the alignment probability from the HMM single word alignment model. Use -1 for unaligned indices that
		/// occur before the first aligned index. Other unaligned indices are indicated by adding the source length to
		/// the previously aligned index.
		/// </summary>
		public double GetAlignmentProbability(int sourceLen, int prevSourceIndex, int sourceIndex)
		{
			CheckDisposed();

			// add 1 to convert the specified indices to Thot position indices, which are 1-based
			return Thot.swAlignModel_getAlignmentProbability(Handle, (uint) (prevSourceIndex + 1), (uint) sourceLen,
				(uint) (sourceIndex + 1));
		}

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			IntPtr nativeSourceSegment = Thot.ConvertStringsToNativeUtf8(sourceSegment);
			IntPtr nativeTargetSegment = Thot.ConvertStringsToNativeUtf8(targetSegment);
			IntPtr nativeMatrix = Thot.AllocNativeMatrix(sourceSegment.Count, targetSegment.Count);

			uint iLen = (uint) sourceSegment.Count;
			uint jLen = (uint) targetSegment.Count;
			try
			{
				Thot.swAlignModel_getBestAlignment(Handle, nativeSourceSegment, nativeTargetSegment, nativeMatrix,
					ref iLen, ref jLen);
				return Thot.ConvertNativeMatrixToWordAlignmentMatrix(nativeMatrix, iLen, jLen);
			}
			finally
			{
				Thot.FreeNativeMatrix(nativeMatrix, iLen);
				Marshal.FreeHGlobal(nativeTargetSegment);
				Marshal.FreeHGlobal(nativeSourceSegment);
			}
		}

		protected override void DisposeUnmanagedResources()
		{
			if (_closeOnDispose)
				Thot.swAlignModel_close(Handle);
		}
	}
}
