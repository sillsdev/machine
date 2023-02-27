using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using SIL.Machine.Corpora;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
    public class ThotSmtModel : DisposableBase, IInteractiveTranslationModel
    {
        private const int MaxDecoderPoolSize = 3;

        private readonly ThotWordAlignmentModel _directWordAlignmentModel;
        private readonly ThotWordAlignmentModel _inverseWordAlignmentModel;
        private readonly SymmetrizedWordAlignmentModel _symmetrizedWordAlignmentModel;
        private readonly ObjectPool<ThotSmtDecoder> _decoderPool;
        private IntPtr _handle;
        private IWordAligner _wordAligner;

        public ThotSmtModel(ThotWordAlignmentModelType wordAlignmentModelType, string cfgFileName)
            : this(wordAlignmentModelType, ThotSmtParameters.Load(cfgFileName))
        {
            ConfigFileName = cfgFileName;
        }

        public ThotSmtModel(ThotWordAlignmentModelType wordAlignmentModelType, ThotSmtParameters parameters)
        {
            _decoderPool = new ObjectPool<ThotSmtDecoder>(MaxDecoderPoolSize, () => new ThotSmtDecoder(this));

            Parameters = parameters;
            Parameters.Freeze();

            WordAlignmentModelType = wordAlignmentModelType;
            _handle = Thot.LoadSmtModel(wordAlignmentModelType, Parameters);

            _directWordAlignmentModel = ThotWordAlignmentModel.Create(wordAlignmentModelType);
            _directWordAlignmentModel.SetHandle(Thot.smtModel_getSingleWordAlignmentModel(_handle), true);

            _inverseWordAlignmentModel = ThotWordAlignmentModel.Create(wordAlignmentModelType);
            _inverseWordAlignmentModel.SetHandle(Thot.smtModel_getInverseSingleWordAlignmentModel(_handle), true);

            _symmetrizedWordAlignmentModel = new SymmetrizedWordAlignmentModel(
                _directWordAlignmentModel,
                _inverseWordAlignmentModel
            );
            WordAligner = new FuzzyEditDistanceWordAlignmentMethod();
        }

        public string ConfigFileName { get; }
        public ThotSmtParameters Parameters { get; private set; }
        public IWordAligner WordAligner
        {
            get => _wordAligner;
            set
            {
                _wordAligner = value;
                if (_wordAligner is IWordAlignmentMethod method)
                    method.ScoreSelector = GetWordAlignmentScore;
            }
        }
        internal IntPtr Handle => _handle;

        public ThotWordAlignmentModel DirectWordAlignmentModel
        {
            get
            {
                CheckDisposed();

                return _directWordAlignmentModel;
            }
        }

        public ThotWordAlignmentModel InverseWordAlignmentModel
        {
            get
            {
                CheckDisposed();

                return _inverseWordAlignmentModel;
            }
        }

        public SymmetrizedWordAlignmentModel SymmetrizedWordAlignmentModel
        {
            get
            {
                CheckDisposed();

                return _symmetrizedWordAlignmentModel;
            }
        }

        public ThotWordAlignmentModelType WordAlignmentModelType { get; }

        public async Task<WordGraph> GetWordGraphAsync(
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            using (
                ObjectPoolItem<ThotSmtDecoder> item = await _decoderPool
                    .GetAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                return item.Object.GetWordGraph(segment);
            }
        }

        public async Task TrainSegmentAsync(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            bool sentenceStart = true,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            using (
                ObjectPoolItem<ThotSmtDecoder> item = await _decoderPool
                    .GetAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                item.Object.TrainSegment(sourceSegment, targetSegment, sentenceStart);
            }
        }

        public async Task<TranslationResult> TranslateAsync(
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            using (
                ObjectPoolItem<ThotSmtDecoder> item = await _decoderPool
                    .GetAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                return item.Object.Translate(segment);
            }
        }

        public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(
            int n,
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            using (
                ObjectPoolItem<ThotSmtDecoder> item = await _decoderPool
                    .GetAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                return item.Object.Translate(n, segment);
            }
        }

        public async Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
            IReadOnlyList<IReadOnlyList<string>> segments,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            var transformBlock = new TransformBlock<IReadOnlyList<string>, TranslationResult>(
                s => TranslateAsync(s, cancellationToken),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = MaxDecoderPoolSize,
                    CancellationToken = cancellationToken
                }
            );

            var bufferBlock = new BufferBlock<TranslationResult>();
            transformBlock.LinkTo(bufferBlock);

            foreach (IReadOnlyList<string> segment in segments)
                transformBlock.Post(segment);
            transformBlock.Complete();

            await transformBlock.Completion.ConfigureAwait(false);

            bufferBlock.TryReceiveAll(out IList<TranslationResult> results);

            return new ReadOnlyList<TranslationResult>(results);
        }

        public async Task<IReadOnlyList<IReadOnlyList<TranslationResult>>> TranslateBatchAsync(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            var transformBlock = new TransformBlock<IReadOnlyList<string>, IReadOnlyList<TranslationResult>>(
                s => TranslateAsync(n, s, cancellationToken),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = MaxDecoderPoolSize,
                    CancellationToken = cancellationToken
                }
            );

            var bufferBlock = new BufferBlock<IReadOnlyList<TranslationResult>>();
            transformBlock.LinkTo(bufferBlock);

            foreach (IReadOnlyList<string> segment in segments)
                transformBlock.Post(segment);
            transformBlock.Complete();

            await transformBlock.Completion.ConfigureAwait(false);

            bufferBlock.TryReceiveAll(out IList<IReadOnlyList<TranslationResult>> results);

            return new ReadOnlyList<IReadOnlyList<TranslationResult>>(results);
        }

        public async Task<TranslationResult> GetBestPhraseAlignmentAsync(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            using (
                ObjectPoolItem<ThotSmtDecoder> item = await _decoderPool
                    .GetAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                return item.Object.GetBestPhraseAlignment(sourceSegment, targetSegment);
            }
        }

        public WordGraph GetWordGraph(IReadOnlyList<string> segment)
        {
            CheckDisposed();

            using (ObjectPoolItem<ThotSmtDecoder> item = _decoderPool.Get())
            {
                return item.Object.GetWordGraph(segment);
            }
        }

        public void TrainSegment(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            bool sentenceStart = true
        )
        {
            CheckDisposed();

            using (ObjectPoolItem<ThotSmtDecoder> item = _decoderPool.Get())
            {
                item.Object.TrainSegment(sourceSegment, targetSegment, sentenceStart);
            }
        }

        public TranslationResult Translate(IReadOnlyList<string> segment)
        {
            CheckDisposed();

            using (ObjectPoolItem<ThotSmtDecoder> item = _decoderPool.Get())
            {
                return item.Object.Translate(segment);
            }
        }

        public IReadOnlyList<TranslationResult> Translate(int n, IReadOnlyList<string> segment)
        {
            CheckDisposed();

            using (ObjectPoolItem<ThotSmtDecoder> item = _decoderPool.Get())
            {
                return item.Object.Translate(n, segment);
            }
        }

        public IReadOnlyList<TranslationResult> TranslateBatch(IReadOnlyList<IReadOnlyList<string>> segments)
        {
            CheckDisposed();

            return segments
                .AsParallel()
                .AsOrdered()
                .WithDegreeOfParallelism(MaxDecoderPoolSize)
                .Select(s => Translate(s))
                .ToArray();
        }

        public IReadOnlyList<IReadOnlyList<TranslationResult>> TranslateBatch(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments
        )
        {
            CheckDisposed();

            return segments
                .AsParallel()
                .AsOrdered()
                .WithDegreeOfParallelism(MaxDecoderPoolSize)
                .Select(s => Translate(n, s))
                .ToArray();
        }

        public TranslationResult GetBestPhraseAlignment(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment
        )
        {
            CheckDisposed();

            using (ObjectPoolItem<ThotSmtDecoder> item = _decoderPool.Get())
            {
                return item.Object.GetBestPhraseAlignment(sourceSegment, targetSegment);
            }
        }

        public Task SaveAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            Save();
            return Task.CompletedTask;
        }

        public void Save()
        {
            CheckDisposed();

            Thot.smtModel_saveModels(_handle);
        }

        public ThotSmtModelTrainer CreateTrainer(IParallelTextCorpus corpus)
        {
            CheckDisposed();

            return string.IsNullOrEmpty(ConfigFileName)
                ? new Trainer(this, corpus, Parameters)
                : new Trainer(this, corpus, ConfigFileName);
        }

        ITrainer ITranslationModel.CreateTrainer(IParallelTextCorpus corpus)
        {
            return CreateTrainer(corpus);
        }

        protected override void DisposeManagedResources()
        {
            _decoderPool.Dispose();
            _directWordAlignmentModel.Dispose();
            _inverseWordAlignmentModel.Dispose();
        }

        protected override void DisposeUnmanagedResources()
        {
            Thot.smtModel_close(_handle);
        }

        private double GetWordAlignmentScore(
            IReadOnlyList<string> sourceSegment,
            int sourceIndex,
            IReadOnlyList<string> targetSegment,
            int targetIndex
        )
        {
            return _symmetrizedWordAlignmentModel.GetTranslationScore(
                sourceIndex == -1 ? null : sourceSegment[sourceIndex],
                targetIndex == -1 ? null : targetSegment[targetIndex]
            );
        }

        private class Trainer : ThotSmtModelTrainer
        {
            private readonly ThotSmtModel _smtModel;

            public Trainer(ThotSmtModel smtModel, IParallelTextCorpus corpus, string cfgFileName)
                : base(smtModel.WordAlignmentModelType, corpus, cfgFileName)
            {
                _smtModel = smtModel;
            }

            public Trainer(ThotSmtModel smtModel, IParallelTextCorpus corpus, ThotSmtParameters parameters)
                : base(smtModel.WordAlignmentModelType, corpus, parameters)
            {
                _smtModel = smtModel;
            }

            public override async Task SaveAsync(CancellationToken cancellationToken)
            {
                await _smtModel._decoderPool.ResetAsync(cancellationToken).ConfigureAwait(false);
                Thot.smtModel_close(_smtModel._handle);

                await base.SaveAsync(cancellationToken).ConfigureAwait(false);

                _smtModel.Parameters = Parameters;
                _smtModel._handle = Thot.LoadSmtModel(_smtModel.WordAlignmentModelType, _smtModel.Parameters);
                _smtModel._directWordAlignmentModel.SetHandle(
                    Thot.smtModel_getSingleWordAlignmentModel(_smtModel._handle),
                    owned: true
                );
                _smtModel._inverseWordAlignmentModel.SetHandle(
                    Thot.smtModel_getInverseSingleWordAlignmentModel(_smtModel._handle),
                    owned: true
                );
            }
        }
    }
}
