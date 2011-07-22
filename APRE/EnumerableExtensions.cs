using System.Collections.Generic;

namespace SIL.APRE
{
	public static class EnumerableExtensions
	{
		public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T item)
		{
			foreach (T i in source)
				yield return i;
			yield return item;
		}
	}
}
