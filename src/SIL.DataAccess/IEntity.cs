namespace SIL.DataAccess;

public interface IEntity
{
    string Id { get; set; }
    int Revision { get; set; }
}
