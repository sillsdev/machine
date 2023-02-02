using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class ScriptureTextCorpus : DictionaryTextCorpus
    {
        public static ScriptureTextCorpus CreateVersificationRefCorpus(ScrVers versification = null)
        {
            if (versification == null)
                versification = ScrVers.Original;

            return new ScriptureTextCorpus(
                versification,
                Enumerable
                    .Range(1, versification.GetLastBook() + 1)
                    .Where(
                        b =>
                            Canon.IsCanonical(b)
                            && (
                                versification.GetLastChapter(b) != 1
                                || versification.GetLastVerse(b, versification.GetLastChapter(b)) != 1
                            )
                            && (b < 87 || b > 92)
                    )
                    .Select(b => new VersificationRefCorpusText(b, versification))
            );
        }

        public ScriptureTextCorpus(ScrVers versification, params IText[] texts) : base(texts)
        {
            Versification = versification;
        }

        public ScriptureTextCorpus(ScrVers versification, IEnumerable<IText> texts) : base(texts)
        {
            Versification = versification;
        }

        protected ScriptureTextCorpus() { }

        public ScrVers Versification { get; protected set; } = ScrVers.English;

        private class VersificationRefCorpusText : ScriptureText
        {
            public VersificationRefCorpusText(int bookNum, ScrVers versification)
                : base(Canon.BookNumberToId(bookNum), versification) { }

            protected override IEnumerable<TextRow> GetVersesInDocOrder()
            {
                int b = Canon.BookIdToNumber(Id);
                for (int c = 1; c <= Versification.GetLastChapter(b); c++)
                {
                    for (int v = 1; v <= Versification.GetLastVerse(b, c); v++)
                    {
                        VerseRef vref = CreateVerseRef(c.ToString(), v.ToString());
                        if (!Versification.IsExcluded(vref.BBBCCCVVV))
                        {
                            foreach (TextRow row in CreateRows(vref))
                                yield return row;
                        }
                    }
                }
            }
        }
    }
}
