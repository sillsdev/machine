namespace SIL.Machine.Corpora
{
    public abstract class ParallelTextCorpusBase : CorpusBase<ParallelTextRow>, IParallelTextCorpus
    {
        public abstract bool IsSourceTokenized { get; }
        public abstract bool IsTargetTokenized { get; }
    }
}
