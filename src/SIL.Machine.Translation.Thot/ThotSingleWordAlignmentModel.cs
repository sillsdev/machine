using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSingleWordAlignmentModel : DisposableBase, ISegmentAligner
	{
		private IntPtr _handle;
		private readonly bool _closeOnDispose;
		private readonly string _prefFileName;

		internal ThotSingleWordAlignmentModel(IntPtr handle)
		{
			_handle = handle;
			_closeOnDispose = false;
		}

		public ThotSingleWordAlignmentModel(string prefFileName, bool createNew = false)
		{
			if (!createNew && !File.Exists(prefFileName + ".src"))
				throw new FileNotFoundException("The single-word alignment model configuration could not be found.");

			_prefFileName = prefFileName;
			_handle = createNew || !File.Exists(prefFileName + ".src") ? Thot.swAlignModel_create() : Thot.swAlignModel_open(_prefFileName);
			_closeOnDispose = true;
		}

		internal IntPtr Handle
		{
			get { return _handle; }
			set { _handle = value; }
		}

		public void AddSegmentPair(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment, WordAlignmentMatrix matrix = null)
		{
			IntPtr nativeSourceSegment = Thot.ConvertStringsToNativeUtf8(sourceSegment);
			IntPtr nativeTargetSegment = Thot.ConvertStringsToNativeUtf8(targetSegment);
			IntPtr nativeMatrix = IntPtr.Zero;
			uint iLen = 0, jLen = 0;
			if (matrix != null)
			{
				nativeMatrix = Thot.ConvertWordAlignmentMatrixToNativeMatrix(matrix);
				iLen = (uint) matrix.RowCount;
				jLen = (uint) matrix.ColumnCount;
			}

			try
			{
				Thot.swAlignModel_addSentencePair(_handle, nativeSourceSegment, nativeTargetSegment, nativeMatrix, iLen, jLen);
			}
			finally
			{
				Thot.FreeNativeMatrix(nativeMatrix, iLen);
				Marshal.FreeHGlobal(nativeTargetSegment);
				Marshal.FreeHGlobal(nativeSourceSegment);
			}
		}

		public void Train(int iterCount)
		{
			Thot.swAlignModel_train(_handle, (uint) iterCount);
		}

		public void Save()
		{
			if (string.IsNullOrEmpty(_prefFileName))
				throw new InvalidOperationException("This single word alignment model cannot be saved.");
			Thot.swAlignModel_save(_handle, _prefFileName);
		}

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			IntPtr nativeSourceWord = Thot.ConvertStringToNativeUtf8(sourceWord ?? "NULL");
			IntPtr nativeTargetWord = Thot.ConvertStringToNativeUtf8(targetWord ?? "NULL");
			try
			{
				return Thot.swAlignModel_getTranslationProbability(_handle, nativeSourceWord, nativeTargetWord);
			}
			finally
			{
				Marshal.FreeHGlobal(nativeTargetWord);
				Marshal.FreeHGlobal(nativeSourceWord);
			}
		}

		public double GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment, out WordAlignmentMatrix matrix)
		{
			IntPtr nativeSourceSegment = Thot.ConvertStringsToNativeUtf8(sourceSegment);
			IntPtr nativeTargetSegment = Thot.ConvertStringsToNativeUtf8(targetSegment);
			IntPtr nativeMatrix = Thot.AllocNativeMatrix(sourceSegment.Count, targetSegment.Count);

			uint iLen = (uint) sourceSegment.Count;
			uint jLen = (uint) targetSegment.Count;
			try
			{
				float prob = Thot.swAlignModel_getBestAlignment(_handle, nativeSourceSegment, nativeTargetSegment, nativeMatrix, ref iLen, ref jLen);

				matrix = Thot.ConvertNativeMatrixToWordAlignmentMatrix(nativeMatrix, iLen, jLen);

				return prob;
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
				Thot.swAlignModel_close(_handle);
		}
	}
}
