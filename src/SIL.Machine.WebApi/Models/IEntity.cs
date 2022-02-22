namespace SIL.Machine.WebApi.Models;

public interface IEntity<T> : ICloneable<T> where T : IEntity<T>
{
	string Id { get; set; }
}
