using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    public abstract class WordAlignParallelTextCorpusBase : ParallelTextCorpusBase
    {
        private readonly IParallelTextCorpus _corpus;

        protected WordAlignParallelTextCorpusBase(IParallelTextCorpus corpus)
        {
            _corpus = corpus;
        }

        public override bool IsSourceTokenized => _corpus.IsSourceTokenized;

        public override bool IsTargetTokenized => _corpus.IsTargetTokenized;

        public override int Count(bool includeEmpty = true, IEnumerable<string> textIds = null) =>
            // Aligning does not add or remove rows, so counting need not align, which may train a model.
            _corpus.Count(includeEmpty, textIds);
    }
}
