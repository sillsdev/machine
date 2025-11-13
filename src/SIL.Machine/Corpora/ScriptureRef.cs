using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    /**
     * A reference to a specific text segment in a scripture text. A verse reference is a the primary anchor point for
     * each text segment. If the text segment is not in a verse, then a path is used to specify the location of the
     * segment within the verse. A path element consists of the position with the parent and a name. The position is
     * 1-based. The position 0 is used when a position is not specified or unknown. The reference is serialized in the
     * following format: "[verse reference]/[path element 1]/[path element 2]/...". A path element is serialized as
     * "[position]:[name]". For example, the reference for the section header that occurs directly after MAT 1:1 would
     * be represented as "MAT 1:1/1:s". Introductory material that occurs at the beginning of a book before the first
     * verse is referenced by the "1:0" verse reference. Some non-verse text segments can be nested in another element.
     * For example, a table cell might be represented as "MAT 1:1/1:tr/1:tc1".
     */
    public class ScriptureRef : IEquatable<ScriptureRef>, IComparable<ScriptureRef>, IComparable
    {
        public static ScriptureRef Empty { get; } = new ScriptureRef();

        public static ScriptureRef Parse(string str, ScrVers versification = null)
        {
            string[] parts = str.Split('/');
            if (parts.Length == 1)
                return new ScriptureRef(new VerseRef(parts[0], versification ?? ScrVers.English));

            string vref = parts[0];
            var path = new List<ScriptureElement>();
            foreach (string part in parts.Skip(1))
            {
                string[] elem = part.Split(':');
                if (elem.Length == 1)
                    path.Add(new ScriptureElement(0, elem[0]));
                else
                    path.Add(new ScriptureElement(int.Parse(elem[0], CultureInfo.InvariantCulture), elem[1]));
            }

            return new ScriptureRef(new VerseRef(vref, versification ?? ScrVers.English), path);
        }

        public static bool TryParse(string str, out ScriptureRef scriptureRef)
        {
            return TryParse(str, null, out scriptureRef);
        }

        public static bool TryParse(string str, ScrVers versification, out ScriptureRef scriptureRef)
        {
            try
            {
                scriptureRef = Parse(str, versification);
                return true;
            }
            catch (VerseRefException)
            {
                scriptureRef = Empty;
                return false;
            }
        }

        public ScriptureRef(VerseRef verseRef = default, IEnumerable<ScriptureElement> path = null)
        {
            VerseRef = verseRef;
            Path = path?.ToArray() ?? Array.Empty<ScriptureElement>();
        }

        public VerseRef VerseRef { get; }
        public IReadOnlyList<ScriptureElement> Path { get; }
        public int BookNum => VerseRef.BookNum;
        public int ChapterNum => VerseRef.ChapterNum;
        public int VerseNum => VerseRef.VerseNum;
        public string Book => VerseRef.Book;
        public string Chapter => VerseRef.Chapter;
        public string Verse => VerseRef.Verse;
        public ScrVers Versification => VerseRef.Versification;
        public bool IsEmpty => VerseRef.IsDefault;
        public bool IsVerse => VerseRef.VerseNum != 0 && Path.Count == 0;

        public ScriptureRef ToRelaxed()
        {
            return new ScriptureRef(VerseRef, Path.Select(pe => pe.ToRelaxed()));
        }

        public ScriptureRef ChangeVersification(ScrVers versification)
        {
            VerseRef vr = VerseRef.Clone();
            vr.ChangeVersification(versification);
            return new ScriptureRef(vr, Path);
        }

        int IComparable<ScriptureRef>.CompareTo(ScriptureRef other)
        {
            return CompareTo(other, compareSegments: true);
        }

        public int CompareTo(ScriptureRef other, bool compareSegments = true)
        {
            IComparer<VerseRef> comparer = compareSegments ? VerseRefComparer.Default : VerseRefComparer.IgnoreSegments;
            int res = comparer.Compare(VerseRef, other.VerseRef);
            if (res != 0)
                return res;

            foreach ((ScriptureElement se1, ScriptureElement se2) in Path.Zip(other.Path))
            {
                res = se1.CompareTo(se2);
                if (res != 0)
                    return res;
            }

            return Path.Count.CompareTo(other.Path.Count);
        }

        public int CompareTo(object obj)
        {
            if (!(obj is ScriptureRef sr))
                throw new ArgumentException("obj is not a ScriptureRef.");

            return CompareTo(sr);
        }

        public bool Equals(ScriptureRef other)
        {
            return VerseRef.Equals(other.VerseRef) && Path.SequenceEqual(other.Path);
        }

        public override bool Equals(object obj)
        {
            return obj is ScriptureRef sr && Equals(sr);
        }

        public override int GetHashCode()
        {
            int hashCode = 23;
            hashCode = hashCode * 31 + VerseRef.GetHashCode();
            hashCode = hashCode * 31 + Path.GetSequenceHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(VerseRef);
            foreach (ScriptureElement se in Path)
                sb.Append($"/{se}");
            return sb.ToString();
        }
    }
}
