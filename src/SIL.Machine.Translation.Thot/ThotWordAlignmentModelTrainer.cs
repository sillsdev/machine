using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotWordAlignmentModelTrainer : ThotWordAlignmentModelTrainer<HmmThotWordAlignmentModel>
	{
		public ThotWordAlignmentModelTrainer(string prefFileName, ITokenProcessor sourcePreprocessor,
			ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
			: base(prefFileName, sourcePreprocessor, targetPreprocessor, corpus, maxCorpusCount)
		{
		}
	}

	public class ThotWordAlignmentModelTrainer<TAlignModel> : DisposableBase, ITrainer
		where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
	{
		private readonly string _prefFileName;
		private readonly ITokenProcessor _sourcePreprocessor;
		private readonly ITokenProcessor _targetPreprocessor;
		private readonly ParallelTextCorpus _parallelCorpus;
		private readonly int _maxCorpusCount;

		public ThotWordAlignmentModelTrainer(string prefFileName, ITokenProcessor sourcePreprocessor,
			ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
		{
			_prefFileName = prefFileName;
			_sourcePreprocessor = sourcePreprocessor;
			_targetPreprocessor = targetPreprocessor;
			_parallelCorpus = corpus;
			_maxCorpusCount = maxCorpusCount;
			string className = Thot.GetWordAlignmentClassName<TAlignModel>();
			Handle = Thot.swAlignModel_create(className);
		}

		public int TrainingIterationCount { get; set; } = 5;
		public Func<ParallelTextSegment, int, bool> SegmentFilter { get; set; } = (s, i) => true;

		protected IntPtr Handle { get; }
		protected bool CloseOnDispose { get; set; } = true;

		public void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
		{
			int corpusCount = 0;
			int index = 0;
			foreach (ParallelTextSegment segment in _parallelCorpus.Segments)
			{
				if (IsSegmentValid(segment))
				{
					if (SegmentFilter(segment, index))
						AddSegmentPair(segment);
					corpusCount++;
				}
				index++;
				if (corpusCount == _maxCorpusCount)
					break;
			}

			for (int i = 0; i < TrainingIterationCount; i++)
			{
				progress?.Report(new ProgressStatus(i, TrainingIterationCount));
				Thot.swAlignModel_train(Handle, 1);
			}
			progress?.Report(new ProgressStatus(1.0));
		}

		public virtual void Save()
		{
			if (!string.IsNullOrEmpty(_prefFileName))
				Thot.swAlignModel_save(Handle, _prefFileName);
		}

		public Task SaveAsync()
		{
			Save();
			return Task.CompletedTask;
		}

		protected override void DisposeManagedResources()
		{
			if (CloseOnDispose)
				Thot.swAlignModel_close(Handle);
		}

		private static bool IsSegmentValid(ParallelTextSegment segment)
		{
			return !segment.IsEmpty && segment.SourceSegment.Count <= TranslationConstants.MaxSegmentLength
				&& segment.TargetSegment.Count <= TranslationConstants.MaxSegmentLength;
		}

		private void AddSegmentPair(ParallelTextSegment segment)
		{
			IReadOnlyList<string> sourceSegment = _sourcePreprocessor.Process(segment.SourceSegment);
			IReadOnlyList<string> targetSegment = _targetPreprocessor.Process(segment.TargetSegment);

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
	}
}
