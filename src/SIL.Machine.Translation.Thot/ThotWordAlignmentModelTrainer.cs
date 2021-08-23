using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotWordAlignmentModelTrainer : ThotWordAlignmentModelTrainer<ThotHmmWordAlignmentModel>
	{
		public ThotWordAlignmentModelTrainer(string prefFileName, ITokenProcessor sourcePreprocessor,
			ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
			: base(prefFileName, sourcePreprocessor, targetPreprocessor, corpus, maxCorpusCount)
		{
		}
	}

	public class ThotWordAlignmentModelTrainer<TAlignModel> : DisposableBase, ITrainer
		where TAlignModel : ThotWordAlignmentModel, new()
	{
		private readonly string _prefFileName;
		private readonly ITokenProcessor _sourcePreprocessor;
		private readonly ITokenProcessor _targetPreprocessor;
		private readonly ParallelTextCorpus _parallelCorpus;
		private readonly int _maxCorpusCount;
		private readonly int _maxSegmentLength = int.MaxValue;

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
			switch (className)
			{
				case Thot.FastAlignWordAlignmentClassName:
					TrainingIterationCount = 4;
					break;

				case Thot.HmmWordAlignmentClassName:
					_maxSegmentLength = 200;
					break;

				case Thot.Ibm1WordAlignmentClassName:
				case Thot.Ibm2WordAlignmentClassName:
					_maxSegmentLength = 1024;
					break;
			}
		}

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

		public Func<ParallelTextSegment, int, bool> SegmentFilter { get; set; } = (s, i) => true;

		protected IntPtr Handle { get; }
		protected bool CloseOnDispose { get; set; } = true;

		public TrainStats Stats { get; } = new TrainStats();

		public void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
		{
			int corpusCount = 0;
			int index = 0;
			int trainedSegmentCount = 0;
			foreach (ParallelTextSegment segment in _parallelCorpus.Segments)
			{
				if (IsSegmentValid(segment))
				{
					if (SegmentFilter(segment, index))
					{
						AddSegmentPair(segment);
						trainedSegmentCount++;
					}
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
			Stats.TrainedSegmentCount = trainedSegmentCount;
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

		private bool IsSegmentValid(ParallelTextSegment segment)
		{
			return !segment.IsEmpty && segment.SourceSegment.Count <= _maxSegmentLength
				&& segment.TargetSegment.Count <= _maxSegmentLength;
		}

		private void AddSegmentPair(ParallelTextSegment segment)
		{
			IReadOnlyList<string> sourceSegment = _sourcePreprocessor.Process(segment.SourceSegment);
			IReadOnlyList<string> targetSegment = _targetPreprocessor.Process(segment.TargetSegment);

			IntPtr nativeSourceSegment = Thot.ConvertSegmentToNativeUtf8(sourceSegment);
			IntPtr nativeTargetSegment = Thot.ConvertSegmentToNativeUtf8(targetSegment);
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
