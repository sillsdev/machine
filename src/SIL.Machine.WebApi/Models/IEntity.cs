namespace SIL.Machine.WebApi.Models;

public interface IEntity<out T> : ICloneable<T> where T : IEntity<T>
{
	string Id { get; set; }
	int Revision { get; set; }
}
