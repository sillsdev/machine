namespace SIL.Machine.Corpora
{
    public interface IAlignmentCollection : ICorpus<AlignmentRow>
    {
        string Id { get; }

        string SortKey { get; }
    }
}
