namespace SIL.Machine.Corpora
{
    public interface IText : ICorpus<TextRow>
    {
        string Id { get; }

        string SortKey { get; }
    }
}
