using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSingleWordAlignmentModel : DisposableBase, ISegmentAligner
	{
		private readonly ThotSmtModel _smtModel;
		private readonly bool _closeOnDispose;
		private readonly string _prefFileName;

		internal ThotSingleWordAlignmentModel(ThotSmtModel smtModel, IntPtr handle)
		{
			_smtModel = smtModel;
			Handle = handle;
			_closeOnDispose = false;
		}

		public ThotSingleWordAlignmentModel(string prefFileName, bool createNew = false)
		{
			if (!createNew && !File.Exists(prefFileName + ".src"))
				throw new FileNotFoundException("The single-word alignment model configuration could not be found.");

			_prefFileName = prefFileName;
			Handle = createNew || !File.Exists(prefFileName + ".src") ? Thot.swAlignModel_create() : Thot.swAlignModel_open(_prefFileName);
			_closeOnDispose = true;
		}

		internal IntPtr Handle { get; set; }

		public void AddSegmentPair(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment, WordAlignmentMatrix hintMatrix = null)
		{
			using (_smtModel?.WriteLock())
			{
				IntPtr nativeSourceSegment = Thot.ConvertStringsToNativeUtf8(sourceSegment);
				IntPtr nativeTargetSegment = Thot.ConvertStringsToNativeUtf8(targetSegment);
				IntPtr nativeMatrix = IntPtr.Zero;
				uint iLen = 0, jLen = 0;
				if (hintMatrix != null)
				{
					nativeMatrix = Thot.ConvertWordAlignmentMatrixToNativeMatrix(hintMatrix);
					iLen = (uint) hintMatrix.RowCount;
					jLen = (uint) hintMatrix.ColumnCount;
				}

				try
				{
					Thot.swAlignModel_addSentencePair(Handle, nativeSourceSegment, nativeTargetSegment, nativeMatrix, iLen, jLen);
				}
				finally
				{
					Thot.FreeNativeMatrix(nativeMatrix, iLen);
					Marshal.FreeHGlobal(nativeTargetSegment);
					Marshal.FreeHGlobal(nativeSourceSegment);
				}
			}
		}

		public void Train(int iterCount)
		{
			using (_smtModel?.WriteLock())
				Thot.swAlignModel_train(Handle, (uint) iterCount);
		}

		public void Save()
		{
			if (string.IsNullOrEmpty(_prefFileName))
				throw new InvalidOperationException("This single word alignment model cannot be saved.");
			Thot.swAlignModel_save(Handle, _prefFileName);
		}

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			using (_smtModel?.ReadLock())
			{
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
		}

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment,
			WordAlignmentMatrix hintMatrix = null)
		{
			using (_smtModel?.ReadLock())
			{
				IntPtr nativeSourceSegment = Thot.ConvertStringsToNativeUtf8(sourceSegment);
				IntPtr nativeTargetSegment = Thot.ConvertStringsToNativeUtf8(targetSegment);
				IntPtr nativeMatrix = hintMatrix == null
					? Thot.AllocNativeMatrix(sourceSegment.Count, targetSegment.Count)
					: Thot.ConvertWordAlignmentMatrixToNativeMatrix(hintMatrix);

				uint iLen = (uint) sourceSegment.Count;
				uint jLen = (uint) targetSegment.Count;
				try
				{
					Thot.swAlignModel_getBestAlignment(Handle, nativeSourceSegment, nativeTargetSegment, nativeMatrix, ref iLen, ref jLen);
					return Thot.ConvertNativeMatrixToWordAlignmentMatrix(nativeMatrix, iLen, jLen);
				}
				finally
				{
					Thot.FreeNativeMatrix(nativeMatrix, iLen);
					Marshal.FreeHGlobal(nativeTargetSegment);
					Marshal.FreeHGlobal(nativeSourceSegment);
				}
			}
		}

		protected override void DisposeUnmanagedResources()
		{
			if (_closeOnDispose)
				Thot.swAlignModel_close(Handle);
		}
	}
}
