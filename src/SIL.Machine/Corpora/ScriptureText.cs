using System.Collections.Generic;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class ScriptureText : TextBase
    {
        protected ScriptureText(string id, ScrVers versification) : base(id, CorporaUtils.GetScriptureTextSortKey(id))
        {
            Versification = versification ?? ScrVers.English;
        }

        public ScrVers Versification { get; }

        public override IEnumerable<TextRow> GetRows()
        {
            var rowList = new List<TextRow>();
            bool outOfOrder = false;
            var prevVerseRef = new VerseRef();
            foreach (TextRow r in GetVersesInDocOrder())
            {
                TextRow row = r;
                var verseRef = (VerseRef)row.Ref;
                rowList.Add(row);
                if (!outOfOrder && verseRef.CompareTo(prevVerseRef) < 0)
                    outOfOrder = true;
                prevVerseRef = verseRef;
            }

            if (outOfOrder)
                rowList.Sort((x, y) => ((VerseRef)x.Ref).CompareTo(y.Ref));
            return rowList;
        }

        protected abstract IEnumerable<TextRow> GetVersesInDocOrder();

        protected IEnumerable<TextRow> CreateRows(
            string chapter,
            string verse,
            string text,
            bool isSentenceStart = true
        )
        {
            foreach (TextRow row in CreateRows(CreateVerseRef(chapter, verse), text, isSentenceStart))
                yield return row;
        }

        protected IEnumerable<TextRow> CreateRows(VerseRef verseRef, string text = "", bool isSentenceStart = true)
        {
            if (verseRef.HasMultiple)
            {
                bool firstVerse = true;
                foreach (VerseRef vref in verseRef.AllVerses())
                {
                    if (firstVerse)
                    {
                        var flags = TextRowFlags.InRange | TextRowFlags.RangeStart;
                        if (isSentenceStart)
                            flags |= TextRowFlags.SentenceStart;
                        yield return CreateRow(text, vref, flags);
                        firstVerse = false;
                    }
                    else
                    {
                        yield return CreateEmptyRow(vref, TextRowFlags.InRange);
                    }
                }
            }
            else
            {
                yield return CreateRow(
                    text,
                    verseRef,
                    isSentenceStart ? TextRowFlags.SentenceStart : TextRowFlags.None
                );
            }
        }

        protected VerseRef CreateVerseRef(string chapter, string verse)
        {
            return new VerseRef(Id, chapter, verse, Versification);
        }
    }
}
