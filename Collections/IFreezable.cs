namespace SIL.Collections
{
	public interface IFreezable<in T>
	{
		bool IsFrozen { get; }
		void Freeze();

		bool ValueEquals(T other);
		int GetFrozenHashCode();
	}
}
