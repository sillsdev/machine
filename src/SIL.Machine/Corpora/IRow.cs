namespace SIL.Machine.Corpora
{
    public interface IRow
    {
        object Ref { get; }

        bool IsEmpty { get; }

        TextRowContentType ContentType { get; }
    }
}
