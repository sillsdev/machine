using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Server.Models
{
	public interface IEntity<out T> : ICloneable<T> where T : IEntity<T>
	{
		string Id { get; set; }
		long Revision { get; set; }
	}
}
