using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotWordAlignmentModelTrainer : DisposableBase, ITrainer
	{
		private readonly string _prefFileName;
		private readonly ITokenProcessor _sourcePreprocessor;
		private readonly ITokenProcessor _targetPreprocessor;
		private readonly ParallelTextCorpus _parallelCorpus;
		private readonly string _sourceFileName;
		private readonly string _targetFileName;
		private readonly int _maxCorpusCount;
		private readonly int _maxSegmentLength = int.MaxValue;
		private Func<ParallelTextSegment, int, bool> _segmentFilter;

		public ThotWordAlignmentModelTrainer(ThotWordAlignmentModelType type, string prefFileName,
			ITokenProcessor sourcePreprocessor, ITokenProcessor targetPreprocessor, string sourceFileName,
			string targetFileName) : this(type, prefFileName, sourcePreprocessor, targetPreprocessor, null)
		{
			_sourceFileName = sourceFileName;
			_targetFileName = targetFileName;
		}

		public ThotWordAlignmentModelTrainer(ThotWordAlignmentModelType type, string prefFileName,
			ITokenProcessor sourcePreprocessor, ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus,
			int maxCorpusCount = int.MaxValue)
		{
			_prefFileName = prefFileName;
			_sourcePreprocessor = sourcePreprocessor;
			_targetPreprocessor = targetPreprocessor;
			_parallelCorpus = corpus;
			_maxCorpusCount = maxCorpusCount;
			string className = Thot.GetWordAlignmentClassName(type);
			Handle = Thot.swAlignModel_create(className);
			_maxSegmentLength = (int)Thot.swAlignModel_getMaxSentenceLength(Handle);
			if (type == ThotWordAlignmentModelType.FastAlign)
				TrainingIterationCount = 4;
			_segmentFilter = (s, i) => true;
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

		public Func<ParallelTextSegment, int, bool> SegmentFilter
		{
			get
			{
				CheckDisposed();
				return _segmentFilter;
			}

			set
			{
				CheckDisposed();
				if (!string.IsNullOrEmpty(_sourceFileName) && !string.IsNullOrEmpty(_targetFileName))
				{
					throw new InvalidOperationException(
						"A segment filter cannot be set when corpus filenames are provided.");
				}
				_segmentFilter = value;
			}
		}

		protected IntPtr Handle { get; }
		protected bool CloseOnDispose { get; set; } = true;

		public TrainStats Stats { get; } = new TrainStats();

		public void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
		{
			progress?.Report(new ProgressStatus(0, TrainingIterationCount + 1));
			if (!string.IsNullOrEmpty(_sourceFileName) && !string.IsNullOrEmpty(_targetFileName))
			{
				Thot.swAlignModel_readSentencePairs(Handle, _sourceFileName, _targetFileName, "");
			}
			else
			{
				int corpusCount = 0;
				int index = 0;
				foreach (ParallelTextSegment segment in _parallelCorpus.Segments)
				{
					if (SegmentFilter(segment, index))
					{
						AddSegmentPair(segment);

						if (IsSegmentValid(segment))
							corpusCount++;
					}
					index++;
					if (corpusCount == _maxCorpusCount)
						break;
				}
			}
			checkCanceled?.Invoke();
			var trainedSegmentCount = (int)Thot.swAlignModel_startTraining(Handle);
			for (int i = 0; i < TrainingIterationCount; i++)
			{
				progress?.Report(new ProgressStatus(i + 1, TrainingIterationCount + 1));
				checkCanceled?.Invoke();
				Thot.swAlignModel_train(Handle, 1);
			}
			Thot.swAlignModel_endTraining(Handle);
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
