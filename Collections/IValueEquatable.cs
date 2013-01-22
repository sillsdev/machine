namespace SIL.Collections
{
	public interface IValueEquatable<in T>
	{
		bool ValueEquals(T other);
		int GetValueHashCode();
	}
}
