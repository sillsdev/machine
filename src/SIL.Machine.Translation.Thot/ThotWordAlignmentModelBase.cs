using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SIL.Extensions;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public abstract class ThotWordAlignmentModelBase<TAlignModel> : DisposableBase, IIbm1WordAlignmentModel
		where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
	{
		private bool _owned;
		private ThotWordVocabulary _sourceWords;
		private ThotWordVocabulary _targetWords;
		private string _prefFileName;
		private readonly string _className;

		protected ThotWordAlignmentModelBase(string className)
		{
			_className = className;
			SetHandle(Thot.swAlignModel_create(_className));
		}

		protected ThotWordAlignmentModelBase(string className, string prefFileName, bool createNew = false)
		{
			_className = className;
			if (createNew || !File.Exists(prefFileName + ".src"))
				CreateNew(prefFileName);
			else
				Load(prefFileName);
		}

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

		public IReadOnlySet<int> SpecialSymbolIndices { get; } = new HashSet<int> { 0, 1, 2 }.ToReadOnlySet();

		public int TrainingIterationCount { get; set; } = 5;

		public bool VariationalBayes
		{
			get
			{
				CheckDisposed();

				return Thot.swAlignModel_getVariationalBayes(Handle);
			}

			set
			{
				CheckDisposed();

				Thot.swAlignModel_setVariationalBayes(Handle, value);
			}
		}

		protected IntPtr Handle { get; set; }

		internal void SetHandle(IntPtr handle, bool owned = false)
		{
			if (!_owned && Handle != IntPtr.Zero)
				Thot.swAlignModel_close(Handle);
			Handle = handle;
			_owned = owned;
			_sourceWords = new ThotWordVocabulary(Handle, true);
			_targetWords = new ThotWordVocabulary(Handle, false);
		}

		public void Load(string prefFileName)
		{
			if (_owned)
				throw new InvalidOperationException("The word alignment model is owned by an SMT model.");
			if (!File.Exists(prefFileName + ".src"))
				throw new FileNotFoundException("The word alignment model configuration could not be found.");

			_prefFileName = prefFileName;
			SetHandle(Thot.swAlignModel_open(_className, _prefFileName));
		}

		public Task LoadAsync(string prefFileName)
		{
			Load(prefFileName);
			return Task.CompletedTask;
		}

		public void CreateNew(string prefFileName)
		{
			if (_owned)
				throw new InvalidOperationException("The word alignment model is owned by an SMT model.");

			_prefFileName = prefFileName;
			SetHandle(Thot.swAlignModel_create(_className));
		}

		public ITrainer CreateTrainer(ITokenProcessor sourcePreprocessor, ITokenProcessor targetPreprocessor,
			ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
		{
			CheckDisposed();

			if (_owned)
			{
				throw new InvalidOperationException(
					"The word alignment model cannot be trained independently of its SMT model.");
			}

			return new Trainer(this, sourcePreprocessor, targetPreprocessor, corpus, maxCorpusCount)
			{
				TrainingIterationCount = TrainingIterationCount,
				VariationalBayes = VariationalBayes
			};
		}

		public Task SaveAsync()
		{
			CheckDisposed();

			Save();
			return Task.CompletedTask;
		}

		public void Save()
		{
			CheckDisposed();

			if (!string.IsNullOrEmpty(_prefFileName))
				Thot.swAlignModel_save(Handle, _prefFileName);
		}

		public double GetTranslationScore(string sourceWord, string targetWord)
		{
			return GetTranslationProbability(sourceWord, targetWord);
		}

		public double GetTranslationScore(int sourceWordIndex, int targetWordIndex)
		{
			return GetTranslationProbability(sourceWordIndex, targetWordIndex);
		}

		public abstract double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
			int prevTargetIndex, int targetIndex);

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			CheckDisposed();

			IntPtr nativeSourceWord = Thot.ConvertTokenToNativeUtf8(sourceWord ?? "NULL");
			IntPtr nativeTargetWord = Thot.ConvertTokenToNativeUtf8(targetWord ?? "NULL");
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

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			IntPtr nativeSourceSegment = Thot.ConvertSegmentToNativeUtf8(sourceSegment);
			IntPtr nativeTargetSegment = Thot.ConvertSegmentToNativeUtf8(targetSegment);
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

		public IEnumerable<(string TargetWord, double Score)> GetTranslations(string sourceWord, double threshold = 0)
		{
			CheckDisposed();

			IntPtr nativeSourceWord = Thot.ConvertTokenToNativeUtf8(sourceWord ?? "NULL");
			IntPtr transHandle = Thot.swAlignModel_getTranslations(Handle, nativeSourceWord, (float)threshold);
			try
			{
				uint transCount = Thot.swAlignTrans_getCount(transHandle);

				var wordIndices = new uint[transCount];
				var probs = new float[transCount];
				Thot.swAlignTrans_getTranslations(transHandle, wordIndices, probs, transCount);

				return wordIndices.Zip(probs, (w, p) => (TargetWords[(int)w], (double)p));
			}
			finally
			{
				Marshal.FreeHGlobal(nativeSourceWord);
				Thot.swAlignTrans_destroy(transHandle);
			}
		}

		public IEnumerable<(int TargetWordIndex, double Score)> GetTranslations(int sourceWordIndex,
			double threshold = 0)
		{
			CheckDisposed();

			IntPtr transHandle = Thot.swAlignModel_getTranslationsByIndex(Handle, (uint)sourceWordIndex,
				(float)threshold);
			try
			{
				uint transCount = Thot.swAlignTrans_getCount(transHandle);

				var wordIndices = new uint[transCount];
				var probs = new float[transCount];
				Thot.swAlignTrans_getTranslations(transHandle, wordIndices, probs, transCount);

				return wordIndices.Zip(probs, (w, p) => ((int)w, (double)p));
			}
			finally
			{
				Thot.swAlignTrans_destroy(transHandle);
			}
		}

		protected override void DisposeUnmanagedResources()
		{
			if (!_owned)
				Thot.swAlignModel_close(Handle);
		}

		private class Trainer : ThotWordAlignmentModelTrainer<TAlignModel>
		{
			private readonly ThotWordAlignmentModelBase<TAlignModel> _model;

			public Trainer(ThotWordAlignmentModelBase<TAlignModel> model, ITokenProcessor sourcePreprocessor,
				ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus, int maxCorpusCount)
				: base(model._prefFileName, sourcePreprocessor, targetPreprocessor, corpus, maxCorpusCount)
			{
				_model = model;
				CloseOnDispose = false;
			}

			public override void Save()
			{
				Thot.swAlignModel_close(_model.Handle);
				_model.Handle = IntPtr.Zero;

				base.Save();

				_model.SetHandle(Handle);
			}
		}
	}
}
