using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public enum EntityChangeType
	{
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
