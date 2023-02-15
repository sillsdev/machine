namespace Serval.AspNetCore.Models;

public interface IOwnedEntity : IEntity
{
    string Owner { get; set; }
}
