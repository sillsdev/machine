namespace SIL.Machine.WebApi.Models;

public interface IOwnedEntity : IEntity
{
    string Owner { get; set; }
}
