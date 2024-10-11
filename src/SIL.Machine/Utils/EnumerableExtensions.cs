using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Utils
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<TResult> ZipMany<TSource, TResult>(
            this IEnumerable<IEnumerable<TSource>> source,
            Func<IEnumerable<TSource>, TResult> selector
        )
        {
            // ToList is necessary to avoid deferred execution
            List<IEnumerator<TSource>> enumerators = source.Select(seq => seq.GetEnumerator()).ToList();
            if (enumerators.Count == 0)
                yield break;
            try
            {
                while (true)
                {
                    foreach (IEnumerator<TSource> e in enumerators)
                    {
                        bool b = e.MoveNext();
                        if (!b)
                            yield break;
                    }
                    // Again, ToList (or ToArray) is necessary to avoid deferred execution
                    yield return selector(enumerators.Select(e => e.Current).ToList());
                }
            }
            finally
            {
                foreach (IEnumerator<TSource> e in enumerators)
                    e.Dispose();
            }
        }
    }
}
