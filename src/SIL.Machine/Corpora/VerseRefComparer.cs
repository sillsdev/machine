using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class VerseRefComparer : IComparer<VerseRef>, IEqualityComparer<VerseRef>
    {
        public static VerseRefComparer Default { get; } = new VerseRefComparer(compareSegments: true);
        public static VerseRefComparer IgnoreSegments { get; } = new VerseRefComparer(compareSegments: false);

        private readonly bool _compareSegments;

        public VerseRefComparer(bool compareSegments = true)
        {
            _compareSegments = compareSegments;
        }

        public int Compare(VerseRef x, VerseRef y)
        {
            if (!x.HasMultiple && !y.HasMultiple)
                return x.CompareTo(y, null, compareAllVerses: false, _compareSegments);

            // Correctly implement comparing all verses in a range or sequence. The implementation of
            // VerseRef.CompareTo() that compares all verses does not handle segments correctly.
            if (x.Versification != y.Versification)
                y.ChangeVersification(x.Versification);
            VerseRef[] xArray = x.AllVerses().ToArray();
            VerseRef[] yArray = y.AllVerses().ToArray();
            foreach ((VerseRef sx, VerseRef sy) in xArray.Zip(yArray))
            {
                int compare = sx.CompareTo(sy, null, compareAllVerses: false, _compareSegments);
                if (compare != 0)
                    return compare;
            }
            return xArray.Length.CompareTo(yArray.Length);
        }

        public bool Equals(VerseRef x, VerseRef y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(VerseRef obj)
        {
            int hashCode = 23;
            hashCode = hashCode * 31 + (_compareSegments ? obj.BBBCCCVVVS.GetHashCode() : obj.BBBCCCVVV.GetHashCode());
            hashCode = hashCode * 31 + obj.Versification.GetHashCode();
            return hashCode;
        }
    }
}
