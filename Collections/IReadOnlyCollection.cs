using System.Collections.Generic;

namespace SIL.Collections
{
	public interface IReadOnlyCollection<out T> : IEnumerable<T>
	{
		int Count { get; }
	}
}
