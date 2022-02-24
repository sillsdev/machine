namespace SIL.Machine.WebApi.DataAccess;

public enum EntityChangeType
{
	None,
	Insert,
	Update,
	Delete
}

public readonly record struct EntityChange<T>(EntityChangeType Type, T? Entity) where T : IEntity<T>;
