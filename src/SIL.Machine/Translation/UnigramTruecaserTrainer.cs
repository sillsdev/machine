using System;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
    public class UnigramTruecaserTrainer : DisposableBase, ITrainer
    {
        private readonly string _modelPath;
        private readonly ITextCorpus _corpus;

        public UnigramTruecaserTrainer(string modelPath, ITextCorpus corpus)
        {
            _modelPath = modelPath;
            _corpus = corpus;
            NewTruecaser = new UnigramTruecaser();
        }

        public UnigramTruecaserTrainer(ITextCorpus corpus)
            : this(null, corpus) { }

        public ITokenizer<string, int, string> Tokenizer { get; set; } = WhitespaceTokenizer.Instance;

        public TrainStats Stats { get; } = new TrainStats();

        protected UnigramTruecaser NewTruecaser { get; }

        public Task TrainAsync(IProgress<ProgressStatus> progress = null, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            int stepCount = 0;
            if (progress != null)
                stepCount = _corpus.Count(includeEmpty: false);
            int currentStep = 0;
            foreach (TextRow row in _corpus.Tokenize(Tokenizer).WhereNonempty())
            {
                cancellationToken.ThrowIfCancellationRequested();
                NewTruecaser.TrainSegment(row);
                currentStep++;
                progress?.Report(new ProgressStatus(currentStep, stepCount));
            }
            Stats.TrainCorpusSize = currentStep;
            return Task.CompletedTask;
        }

        public virtual async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            if (_modelPath != null)
                await NewTruecaser.SaveAsync(_modelPath, cancellationToken).ConfigureAwait(false);
        }
    }
}
