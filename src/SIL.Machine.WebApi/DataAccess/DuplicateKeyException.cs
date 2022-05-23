namespace SIL.Machine.WebApi.DataAccess;

public class DuplicateKeyException : Exception
{
    private const string DefaultMessage = "The inserted/updated entity has the same key as an existing entity.";

    public DuplicateKeyException() : base(DefaultMessage) { }

    public DuplicateKeyException(Exception innerException) : base(DefaultMessage, innerException) { }
}
