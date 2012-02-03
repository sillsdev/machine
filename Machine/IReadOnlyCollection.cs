using System.Collections.Generic;

namespace SIL.Machine
{
	public interface IReadOnlyCollection<out T> : IEnumerable<T>
	{
		int Count { get; }
	}
}
