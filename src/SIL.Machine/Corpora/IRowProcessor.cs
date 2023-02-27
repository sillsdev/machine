namespace SIL.Machine.Corpora
{
    public interface IRowProcessor<T>
        where T : IRow
    {
        T Process(T row);
    }
}
