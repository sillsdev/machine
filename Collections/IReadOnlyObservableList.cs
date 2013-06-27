namespace SIL.Collections
{
	public interface IReadOnlyObservableList<out T> : IReadOnlyList<T>, IReadOnlyObservableCollection<T>
	{
	}
}
