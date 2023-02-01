using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
    public class UnigramTruecaserTrainer : DisposableBase, ITrainer
    {
        private readonly string _modelPath;
        private readonly IEnumerable<TextRow> _corpus;

        public UnigramTruecaserTrainer(string modelPath, IEnumerable<TextRow> corpus)
        {
            _modelPath = modelPath;
            _corpus = corpus;
            NewTruecaser = new UnigramTruecaser();
        }

        public UnigramTruecaserTrainer(IEnumerable<TextRow> corpus) : this(null, corpus) { }

        public TrainStats Stats { get; } = new TrainStats();

        protected UnigramTruecaser NewTruecaser { get; }

        public Task TrainAsync(IProgress<ProgressStatus> progress = null, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            int stepCount = 0;
            if (progress != null)
                stepCount = _corpus.Count();
            int currentStep = 0;
            foreach (TextRow row in _corpus)
            {
                cancellationToken.ThrowIfCancellationRequested();
                NewTruecaser.TrainSegment(row);
                currentStep++;
                if (progress != null)
                    progress.Report(new ProgressStatus(currentStep, stepCount));
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
