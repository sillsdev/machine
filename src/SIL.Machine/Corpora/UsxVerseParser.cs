using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SIL.Machine.Utils;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class UsxVerseParser
    {
        private static readonly HashSet<string> s_nonVerseParaStyles = new HashSet<string>
        {
            "ms",
            "mr",
            "s",
            "sr",
            "r",
            "d",
            "sp",
            "rem",
            "restore",
            "cl"
        };

        public IEnumerable<UsxVerse> Parse(Stream stream)
        {
            var context = new ParseContext();
            var doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            XElement bookElem = doc.Descendants("book").First();
            XElement rootElem = bookElem.Parent;
            foreach (UsxVerse verse in ParseElement(rootElem, context))
                yield return verse;

            if (context.IsInVerse)
                yield return context.CreateVerse();
        }

        private IEnumerable<UsxVerse> ParseElement(XElement elem, ParseContext context)
        {
            foreach (XNode node in elem.Nodes())
            {
                switch (node)
                {
                    case XElement e:
                        switch (e.Name.LocalName)
                        {
                            case "chapter":
                                if (context.IsInVerse)
                                    yield return context.CreateVerse();
                                context.Chapter = (string)e.Attribute("number");
                                context.Verse = null;
                                context.IsSentenceStart = true;
                                break;

                            case "para":
                                if (!IsVersePara(e))
                                {
                                    context.IsSentenceStart = true;
                                    continue;
                                }
                                context.ParaElement = e;
                                foreach (UsxVerse evt in ParseElement(e, context))
                                    yield return evt;
                                break;

                            case "verse":
                                if (e.Attribute("eid") != null)
                                {
                                    yield return context.CreateVerse();
                                    context.Verse = null;
                                }
                                else
                                {
                                    string verse = (string)e.Attribute("number") ?? (string)e.Attribute("pubnumber");
                                    if (context.IsInVerse)
                                    {
                                        if (verse == context.Verse)
                                        {
                                            yield return context.CreateVerse();

                                            // ignore duplicate verse
                                            context.Verse = null;
                                        }
                                        else if (VerseRef.AreOverlappingVersesRanges(verse, context.Verse))
                                            // merge overlapping verse ranges in to one range
                                            context.Verse = CorporaUtils.MergeVerseRanges(verse, context.Verse);
                                        else
                                        {
                                            yield return context.CreateVerse();
                                            context.Verse = verse;
                                        }
                                    }
                                    else
                                        context.Verse = verse;
                                }
                                break;

                            case "char":
                                if ((string)e.Attribute("style") == "rq")
                                {
                                    if (context.IsInVerse)
                                        context.AddToken("", e);
                                }
                                else
                                    foreach (UsxVerse evt in ParseElement(e, context))
                                        yield return evt;
                                break;

                            case "wg":
                                if (context.IsInVerse)
                                    context.AddToken(e.Value, e);
                                break;

                            case "figure":
                                if (context.IsInVerse)
                                    context.AddToken("", e);
                                break;
                        }
                        break;

                    case XText text:
                        if (context.IsInVerse)
                            context.AddToken(text.Value);
                        break;
                }
            }
        }

        private static bool IsVersePara(XElement paraElem)
        {
            string style = (string)paraElem.Attribute("style");
            if (s_nonVerseParaStyles.Contains(style))
                return false;

            if (IsNumberedStyle("ms", style))
                return false;

            if (IsNumberedStyle("s", style))
                return false;

            return true;
        }

        private static bool IsNumberedStyle(string stylePrefix, string style)
        {
            return style.StartsWith(stylePrefix) && int.TryParse(style.Substring(stylePrefix.Length), out _);
        }

        private class ParseContext
        {
            private readonly List<UsxToken> _tokens = new List<UsxToken>();

            public string Chapter { get; set; }
            public string Verse { get; set; }
            public bool IsInVerse => Chapter != null && Verse != null;
            public bool IsSentenceStart { get; set; } = true;
            public XElement ParaElement { get; set; }

            public void AddToken(string text, XElement elem = null)
            {
                _tokens.Add(new UsxToken(ParaElement, text, elem));
            }

            public UsxVerse CreateVerse()
            {
                var verse = new UsxVerse(Chapter, Verse, IsSentenceStart, _tokens);
                IsSentenceStart = verse.Text.HasSentenceEnding();
                _tokens.Clear();
                return verse;
            }
        }
    }
}
