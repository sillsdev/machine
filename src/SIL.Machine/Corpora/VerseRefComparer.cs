using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class VerseRefComparer : IComparer<VerseRef>
	{
		public static VerseRefComparer Instance => new VerseRefComparer();

		public int Compare(VerseRef x, VerseRef y)
		{
			if (!x.HasMultiple && !y.HasMultiple)
				return x.CompareTo(y);

			// Correctly implement comparing all verses in a range or sequence. The implementation of
			// VerseRef.CompareTo() that compares all verses does not handle segments correctly.
			if (x.Versification != y.Versification)
				y.ChangeVersification(x.Versification);
			VerseRef[] xArray = x.AllVerses().ToArray();
			VerseRef[] yArray = y.AllVerses().ToArray();
			foreach ((VerseRef sx, VerseRef sy) in xArray.Zip(yArray))
			{
				int compare = sx.CompareTo(sy);
				if (compare != 0)
					return compare;
			}
			return xArray.Length.CompareTo(yArray.Length);
		}
	}
}
