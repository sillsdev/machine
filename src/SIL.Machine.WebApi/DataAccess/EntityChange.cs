using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
{
	public enum EntityChangeType
	{
		None,
		Insert,
		Update,
		Delete
	}

	public struct EntityChange<T> where T : class, IEntity<T>
	{
		public EntityChange(EntityChangeType type, T entity)
		{
			Type = type;
			Entity = entity;
		}

		public EntityChangeType Type { get; }
		public T Entity { get; }
	}
}
