using System.Collections.Generic;
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

        protected IEnumerable<TextRow> CreateRows(
            IReadOnlyList<ScriptureRef> scriptureRefs,
            string text = "",
            bool isSentenceStart = true
        )
        {
            if (scriptureRefs.Count > 1)
            {
                bool firstVerse = true;
                foreach (ScriptureRef scriptureRef in scriptureRefs)
                {
                    if (firstVerse)
                    {
                        TextRowFlags flags = TextRowFlags.InRange | TextRowFlags.RangeStart;
                        if (isSentenceStart)
                            flags |= TextRowFlags.SentenceStart;
                        yield return CreateRow(text, scriptureRef, flags);
                        firstVerse = false;
                    }
                    else
                    {
                        yield return CreateEmptyRow(scriptureRef, TextRowFlags.InRange);
                    }
                }
            }
            else
            {
                yield return CreateRow(
                    text,
                    scriptureRefs[0],
                    isSentenceStart ? TextRowFlags.SentenceStart : TextRowFlags.None
                );
            }
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
            IEnumerable<ScriptureElement> elements,
            string text = "",
            bool isSentenceStart = true
        )
        {
            return CreateRow(
                text,
                new ScriptureRef(verseRef, elements),
                isSentenceStart ? TextRowFlags.SentenceStart : TextRowFlags.None
            );
        }

        protected TextRow CreateRow(ScriptureRef scriptureRef, string text = "", bool isSentenceStart = true)
        {
            return CreateRow(text, scriptureRef, isSentenceStart ? TextRowFlags.SentenceStart : TextRowFlags.None);
        }

        protected VerseRef CreateVerseRef(string chapter, string verse)
        {
            return new VerseRef(Id, chapter, verse, Versification);
        }
    }
}
