using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
    public class ThotWordAlignmentModelTrainer : DisposableBase, ITrainer
    {
        private readonly string _prefFileName;
        private readonly IParallelTextCorpus _parallelCorpus;
        private readonly string _sourceFileName;
        private readonly string _targetFileName;
        private readonly int _maxSegmentLength = int.MaxValue;
        private readonly List<(IntPtr Handle, int IterationCount)> _models;
        private readonly bool _isEflomal;

        public ThotWordAlignmentModelTrainer(
            ThotWordAlignmentModelType modelType,
            string sourceFileName,
            string targetFileName,
            string prefFileName,
            ThotWordAlignmentParameters parameters = null
        )
            : this(modelType, null, prefFileName, parameters)
        {
            _sourceFileName = sourceFileName;
            _targetFileName = targetFileName;
        }

        public ThotWordAlignmentModelTrainer(
            ThotWordAlignmentModelType modelType,
            IParallelTextCorpus corpus,
            string prefFileName,
            ThotWordAlignmentParameters parameters = null
        )
        {
            _prefFileName = prefFileName;
            _parallelCorpus = corpus;

            if (parameters == null)
                parameters = new ThotWordAlignmentParameters();

            _models = new List<(IntPtr, int)>();
            if (modelType == ThotWordAlignmentModelType.FastAlign)
            {
                IntPtr fastAlign = Thot.CreateAlignmentModel(modelType);
                Thot.swAlignModel_setVariationalBayes(fastAlign, parameters.GetVariationalBayes(modelType));
                if (parameters.FastAlignP0 != null)
                    Thot.swAlignModel_setFastAlignP0(fastAlign, parameters.FastAlignP0.Value);
                _models.Add((fastAlign, parameters.GetFastAlignIterationCount(modelType)));
            }
            else if (modelType == ThotWordAlignmentModelType.Eflomal)
            {
                // Eflomal is a single model that runs its own Bayesian IBM1->HMM->fertility cascade.
                _isEflomal = true;
                IntPtr eflomal = Thot.CreateAlignmentModel(modelType);
                if (eflomal == IntPtr.Zero)
                {
                    throw new NotSupportedException(
                        "Eflomal alignment model is not supported by the installed Thot native library. "
                            + "A Thot build that includes EflomalAlignmentModel (model type 9) is required."
                    );
                }
                if (parameters.EflomalSeed.HasValue)
                    Thot.swAlignModel_setEflomalSeed(eflomal, parameters.EflomalSeed.Value);
                if (parameters.EflomalNumSamplers.HasValue)
                    Thot.swAlignModel_setEflomalNumSamplers(eflomal, parameters.EflomalNumSamplers.Value);
                if (parameters.EflomalDeterministic.HasValue)
                    Thot.swAlignModel_setEflomalDeterministic(eflomal, parameters.EflomalDeterministic.Value);
                if (parameters.EflomalLexNorm.HasValue)
                    Thot.swAlignModel_setEflomalLexNorm(eflomal, parameters.EflomalLexNorm.Value);
                if (parameters.IsEflomalScheduleSpecified)
                {
                    // An explicit schedule turns off the model's automatic (corpus-scaled) schedule.
                    Thot.swAlignModel_setEflomalIterations(
                        eflomal,
                        parameters.GetEflomalIbm1IterationCount(),
                        parameters.GetEflomalHmmIterationCount(),
                        parameters.GetEflomalFertilityIterationCount()
                    );
                }
                if (parameters.EflomalLexAlpha.HasValue)
                    Thot.swAlignModel_setEflomalAlphaLex(eflomal, parameters.EflomalLexAlpha.Value);
                if (parameters.EflomalJumpAlpha.HasValue)
                    Thot.swAlignModel_setEflomalAlphaJump(eflomal, parameters.EflomalJumpAlpha.Value);
                if (parameters.EflomalFertilityAlpha.HasValue)
                    Thot.swAlignModel_setEflomalAlphaFertility(eflomal, parameters.EflomalFertilityAlpha.Value);
                if (parameters.EflomalP0.HasValue)
                    Thot.swAlignModel_setEflomalP0(eflomal, parameters.EflomalP0.Value);
                if (parameters.EflomalJumpWindow.HasValue)
                    Thot.swAlignModel_setEflomalJumpWindow(eflomal, parameters.EflomalJumpWindow.Value);
                // With an explicit schedule the total sweep count is known up front; with the
                // automatic (corpus-scaled) schedule it is 0 here and resolved from the model after
                // startTraining (see TrainAsync).
                int eflomalIterationCount = parameters.IsEflomalScheduleSpecified
                    ? parameters.GetEflomalIbm1IterationCount()
                        + parameters.GetEflomalHmmIterationCount()
                        + parameters.GetEflomalFertilityIterationCount()
                    : 0;
                _models.Add((eflomal, eflomalIterationCount));
            }
            else
            {
                IntPtr ibm1 = Thot.CreateAlignmentModel(ThotWordAlignmentModelType.Ibm1);
                Thot.swAlignModel_setVariationalBayes(ibm1, parameters.GetVariationalBayes(modelType));
                _models.Add((ibm1, parameters.GetIbm1IterationCount(modelType)));

                IntPtr ibm2OrHmm = IntPtr.Zero;
                if (modelType >= ThotWordAlignmentModelType.Ibm2)
                {
                    if (parameters.GetHmmIterationCount(modelType) > 0)
                    {
                        ibm2OrHmm = Thot.CreateAlignmentModel(ThotWordAlignmentModelType.Hmm, ibm1);
                        if (parameters.HmmP0 != null)
                            Thot.swAlignModel_setHmmP0(ibm2OrHmm, parameters.HmmP0.Value);
                        if (parameters.HmmLexicalSmoothingFactor != null)
                        {
                            Thot.swAlignModel_setHmmLexicalSmoothingFactor(
                                ibm2OrHmm,
                                parameters.HmmLexicalSmoothingFactor.Value
                            );
                        }
                        if (parameters.HmmAlignmentSmoothingFactor != null)
                        {
                            Thot.swAlignModel_setHmmAlignmentSmoothingFactor(
                                ibm2OrHmm,
                                parameters.HmmAlignmentSmoothingFactor.Value
                            );
                        }
                        _models.Add((ibm2OrHmm, parameters.GetHmmIterationCount(modelType)));
                    }
                    else
                    {
                        ibm2OrHmm = Thot.CreateAlignmentModel(ThotWordAlignmentModelType.Ibm2, ibm1);
                        _models.Add((ibm2OrHmm, parameters.GetIbm2IterationCount(modelType)));
                    }
                }

                IntPtr ibm3 = IntPtr.Zero;
                if (
                    modelType >= ThotWordAlignmentModelType.Ibm3
                    && ibm2OrHmm != IntPtr.Zero
                    && parameters.GetIbm3IterationCount(modelType) > 0
                )
                {
                    ibm3 = Thot.CreateAlignmentModel(ThotWordAlignmentModelType.Ibm3, ibm2OrHmm);
                    if (parameters.Ibm3FertilitySmoothingFactor != null)
                    {
                        Thot.swAlignModel_setIbm3FertilitySmoothingFactor(
                            ibm3,
                            parameters.Ibm3FertilitySmoothingFactor.Value
                        );
                    }
                    if (parameters.Ibm3CountThreshold != null)
                        Thot.swAlignModel_setIbm3CountThreshold(ibm3, parameters.Ibm3CountThreshold.Value);
                    _models.Add((ibm3, parameters.GetIbm3IterationCount(modelType)));
                }

                if (modelType >= ThotWordAlignmentModelType.Ibm4)
                {
                    IntPtr ibm4 = IntPtr.Zero;
                    if (ibm3 != IntPtr.Zero)
                        ibm4 = Thot.CreateAlignmentModel(ThotWordAlignmentModelType.Ibm4, ibm3);
                    else if (parameters.GetHmmIterationCount(modelType) > 0)
                        ibm4 = Thot.CreateAlignmentModel(ThotWordAlignmentModelType.Ibm4, ibm2OrHmm);
                    if (ibm4 != IntPtr.Zero)
                    {
                        if (parameters.Ibm4DistortionSmoothingFactor != null)
                        {
                            Thot.swAlignModel_setIbm4DistortionSmoothingFactor(
                                ibm4,
                                parameters.Ibm4DistortionSmoothingFactor.Value
                            );
                        }
                        AddWordClasses(ibm4, parameters.SourceWordClasses, isSource: true);
                        AddWordClasses(ibm4, parameters.TargetWordClasses, isSource: false);
                        _models.Add((ibm4, parameters.GetIbm4IterationCount(modelType)));
                    }
                }
            }

            _maxSegmentLength = (int)Thot.swAlignModel_getMaxSentenceLength(Handle);
        }

        protected IntPtr Handle => _models.Count == 0 ? IntPtr.Zero : _models[_models.Count - 1].Handle;
        protected bool CloseOnDispose { get; set; } = true;

        public ITokenizer<string, int, string> SourceTokenizer { get; set; } = WhitespaceTokenizer.Instance;
        public ITokenizer<string, int, string> TargetTokenizer { get; set; } = WhitespaceTokenizer.Instance;

        public TrainStats Stats { get; } = new TrainStats();

        public int MaxCorpusCount { get; set; } = int.MaxValue;

        public Task TrainAsync(IProgress<ProgressStatus> progress = null, CancellationToken cancellationToken = default)
        {
            // One step to load the corpus, then for each trained model one step to start training plus
            // one per training iteration. When the Eflomal model uses its automatic schedule, the
            // iteration count is derived from the corpus during startTraining (stored as 0 until then),
            // so the total step count is not known up front; progress is reported as indeterminate until
            // it is resolved below.
            bool iterationCountKnown = !_isEflomal || _models[0].IterationCount > 0;
            int? numSteps = iterationCountKnown
                ? _models.Select(m => m.IterationCount).Where(ic => ic > 0).Sum(ic => ic + 1) + 1
                : (int?)null;
            int curStep = 0;

            void Report() =>
                progress?.Report(
                    numSteps.HasValue ? new ProgressStatus(curStep, numSteps.Value) : new ProgressStatus(curStep)
                );

            Report();

            if (!string.IsNullOrEmpty(_sourceFileName) && !string.IsNullOrEmpty(_targetFileName))
            {
                Thot.swAlignModel_readSentencePairs(Handle, _sourceFileName, _targetFileName, "");
            }
            else
            {
                int corpusCount = 0;
                int index = 0;
                foreach (ParallelTextRow row in _parallelCorpus.Tokenize(SourceTokenizer, TargetTokenizer))
                {
                    AddSegmentPair(row);

                    if (IsSegmentValid(row))
                        corpusCount++;

                    index++;
                    if (corpusCount == MaxCorpusCount)
                        break;
                }
            }
            curStep++;
            Report();
            cancellationToken.ThrowIfCancellationRequested();

            int trainedSegmentCount = 0;
            foreach ((IntPtr handle, int storedIterationCount) in _models)
            {
                if (storedIterationCount == 0 && !_isEflomal)
                    continue;

                trainedSegmentCount = (int)Thot.swAlignModel_startTraining(handle);

                int iterationCount = storedIterationCount;
                if (_isEflomal && storedIterationCount == 0)
                {
                    // Automatic schedule: the corpus-scaled sweep count is resolved during startTraining;
                    // ask the model how many sweeps to run and finalize the total step count now.
                    iterationCount = Thot.swAlignModel_getEflomalScheduledIterations(handle);
                    numSteps = curStep + iterationCount + 1;
                }

                curStep++;
                Report();
                cancellationToken.ThrowIfCancellationRequested();

                for (int i = 0; i < iterationCount; i++)
                {
                    Thot.swAlignModel_train(handle, 1);
                    curStep++;
                    Report();
                    cancellationToken.ThrowIfCancellationRequested();
                }
                Thot.swAlignModel_endTraining(handle);
            }
            Stats.TrainCorpusSize = trainedSegmentCount;

            return Task.CompletedTask;
        }

        public virtual void Save()
        {
            if (!string.IsNullOrEmpty(_prefFileName))
                Thot.swAlignModel_save(Handle, _prefFileName);
        }

        public Task SaveAsync(CancellationToken cancellationToken = default)
        {
            Save();
            return Task.CompletedTask;
        }

        protected override void DisposeManagedResources()
        {
            for (int i = 0; i < _models.Count - 1; i++)
                Thot.swAlignModel_close(_models[i].Handle);

            if (CloseOnDispose)
                Thot.swAlignModel_close(Handle);
        }

        private bool IsSegmentValid(ParallelTextRow row)
        {
            return !row.IsEmpty
                && row.SourceSegment.Count <= _maxSegmentLength
                && row.TargetSegment.Count <= _maxSegmentLength;
        }

        private void AddSegmentPair(ParallelTextRow row)
        {
            IntPtr nativeSourceSegment = Thot.ConvertSegmentToNativeUtf8(row.SourceSegment);
            IntPtr nativeTargetSegment = Thot.ConvertSegmentToNativeUtf8(row.TargetSegment);
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

        private static void AddWordClasses(
            IntPtr swAlignModelHandle,
            IReadOnlyDictionary<string, string> wordClasses,
            bool isSource
        )
        {
            foreach (KeyValuePair<string, string> kvp in wordClasses)
            {
                IntPtr nativeWord = Thot.ConvertTokenToNativeUtf8(kvp.Key);
                IntPtr nativeWordClass = Thot.ConvertTokenToNativeUtf8(kvp.Value);
                try
                {
                    if (isSource)
                        Thot.swAlignModel_mapSourceWordToWordClass(swAlignModelHandle, nativeWord, nativeWordClass);
                    else
                        Thot.swAlignModel_mapTargetWordToWordClass(swAlignModelHandle, nativeWord, nativeWordClass);
                }
                finally
                {
                    Marshal.FreeHGlobal(nativeWordClass);
                    Marshal.FreeHGlobal(nativeWord);
                }
            }
        }
    }
}
