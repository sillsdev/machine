namespace SIL.Machine.Corpora
{
    public interface IParallelTextCorpus : ICorpus<ParallelTextRow>
    {
        bool IsSourceTokenized { get; }
        bool IsTargetTokenized { get; }
    }
}
