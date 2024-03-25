using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class ScriptureText : TextBase
    {
        protected ScriptureText(string id, ScrVers versification)
            : base(id, CorporaUtils.GetScriptureTextSortKey(id))
        {
            Versification = versification ?? ScrVers.English;
        }

        public ScrVers Versification { get; }

        public override IEnumerable<TextRow> GetRows()
        {
            var rowList = new List<TextRow>();
            bool outOfOrder = false;
            var prevScrRef = new ScriptureRef();
            foreach (TextRow r in GetVersesInDocOrder())
            {
                TextRow row = r;
                var scrRef = (ScriptureRef)row.Ref;
                rowList.Add(row);
                if (!outOfOrder && scrRef.CompareTo(prevScrRef) < 0)
                    outOfOrder = true;
                prevScrRef = scrRef;
            }

            if (outOfOrder)
                rowList.Sort((x, y) => ((ScriptureRef)x.Ref).CompareTo(y.Ref));
            return rowList;
        }

        protected abstract IEnumerable<TextRow> GetVersesInDocOrder();

        protected IEnumerable<TextRow> CreateRows(VerseRef verseRef, string text = "", bool isSentenceStart = true)
        {
            if (verseRef.HasMultiple)
            {
                bool firstVerse = true;
                foreach (VerseRef vref in verseRef.AllVerses())
                {
                    if (firstVerse)
                    {
                        TextRowFlags flags = TextRowFlags.InRange | TextRowFlags.RangeStart;
                        if (isSentenceStart)
                            flags |= TextRowFlags.SentenceStart;
                        yield return CreateRow(text, new ScriptureRef(vref), flags);
                        firstVerse = false;
                    }
                    else
                    {
                        yield return CreateEmptyRow(new ScriptureRef(vref), TextRowFlags.InRange);
                    }
                }
            }
            else
            {
                yield return CreateRow(
                    text,
                    new ScriptureRef(verseRef),
                    isSentenceStart ? TextRowFlags.SentenceStart : TextRowFlags.None
                );
            }
        }

        protected TextRow CreateRow(
            VerseRef verseRef,
            IEnumerable<(int Position, string Marker)> markers,
            string text = "",
            bool isSentenceStart = true
        )
        {
            return CreateRow(
                text,
                new ScriptureRef(verseRef, markers.Select(m => new ScriptureElement(m.Position, m.Marker))),
                isSentenceStart ? TextRowFlags.SentenceStart : TextRowFlags.None
            );
        }

        protected VerseRef CreateVerseRef(string chapter, string verse)
        {
            return new VerseRef(Id, chapter, verse, Versification);
        }
    }
}
