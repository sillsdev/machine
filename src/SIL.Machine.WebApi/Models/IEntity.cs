namespace SIL.Machine.WebApi.Models;

public interface IEntity
{
	string Id { get; set; }
	int Revision { get; set; }
}
