using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SIL.Machine.Utils;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class UsxVerseParser
    {
        private static readonly HashSet<string> VerseParaStyles = new HashSet<string>
        {
            // Paragraphs
            "p",
            "m",
            "po",
            "pr",
            "cls",
            "pmo",
            "pm",
            "pmc",
            "pmr",
            "pi",
            "pc",
            "mi",
            "nb",
            // Poetry
            "q",
            "qc",
            "qr",
            "qm",
            "qd",
            "b",
            "d",
            // Lists
            "lh",
            "li",
            "lf",
            "lim",
            // Deprecated
            "ph",
            "phi",
            "ps",
            "psi",
        };

        public IEnumerable<UsxVerse> Parse(Stream stream)
        {
            var ctxt = new ParseContext();
            var doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            XElement bookElem = doc.Descendants("book").First();
            XElement rootElem = bookElem.Parent;
            foreach (UsxVerse verse in ParseElement(rootElem, ctxt))
                yield return verse;

            if (ctxt.IsInVerse)
                yield return ctxt.CreateVerse();
        }

        private IEnumerable<UsxVerse> ParseElement(XElement elem, ParseContext ctxt)
        {
            foreach (XNode node in elem.Nodes())
            {
                switch (node)
                {
                    case XElement e:
                        switch (e.Name.LocalName)
                        {
                            case "chapter":
                                if (ctxt.IsInVerse)
                                    yield return ctxt.CreateVerse();
                                ctxt.Chapter = (string)e.Attribute("number");
                                ctxt.Verse = null;
                                ctxt.IsSentenceStart = true;
                                break;

                            case "para":
                                if (!IsVersePara(e))
                                {
                                    ctxt.IsSentenceStart = true;
                                    continue;
                                }
                                ctxt.ParentElement = e;
                                foreach (UsxVerse evt in ParseElement(e, ctxt))
                                    yield return evt;
                                break;

                            case "verse":
                                if (e.Attribute("eid") != null)
                                {
                                    yield return ctxt.CreateVerse();
                                    ctxt.Verse = null;
                                }
                                else
                                {
                                    string verse = (string)e.Attribute("number") ?? (string)e.Attribute("pubnumber");
                                    if (ctxt.IsInVerse)
                                    {
                                        if (verse == ctxt.Verse)
                                        {
                                            yield return ctxt.CreateVerse();

                                            // ignore duplicate verse
                                            ctxt.Verse = null;
                                        }
                                        else if (VerseRef.AreOverlappingVersesRanges(verse, ctxt.Verse))
                                        {
                                            // merge overlapping verse ranges in to one range
                                            ctxt.Verse = CorporaUtils.MergeVerseRanges(verse, ctxt.Verse);
                                        }
                                        else
                                        {
                                            yield return ctxt.CreateVerse();
                                            ctxt.Verse = verse;
                                        }
                                    }
                                    else
                                    {
                                        ctxt.Verse = verse;
                                    }
                                }
                                break;

                            case "char":
                                if ((string)e.Attribute("style") == "rq")
                                {
                                    if (ctxt.IsInVerse)
                                        ctxt.AddToken("", e);
                                }
                                else
                                {
                                    foreach (UsxVerse evt in ParseElement(e, ctxt))
                                        yield return evt;
                                }
                                break;

                            case "wg":
                                if (ctxt.IsInVerse)
                                    ctxt.AddToken(e.Value, e);
                                break;

                            case "figure":
                                if (ctxt.IsInVerse)
                                    ctxt.AddToken("", e);
                                break;
                            case "table":
                                foreach (UsxVerse evt in ParseElement(e, ctxt))
                                    yield return evt;
                                break;
                            case "row":
                                foreach (UsxVerse evt in ParseElement(e, ctxt))
                                    yield return evt;
                                break;
                            case "cell":
                                ctxt.ParentElement = e;
                                foreach (UsxVerse evt in ParseElement(e, ctxt))
                                    yield return evt;
                                break;
                        }
                        break;

                    case XText text:
                        if (ctxt.IsInVerse)
                            ctxt.AddToken(text.Value);
                        break;
                }
            }
        }

        private static bool IsVersePara(XElement parentElement)
        {
            string style = (string)parentElement.Attribute("style");
            // strip any digits to the right of the style name using regular expression
            style = Regex.Replace(style, @"\d+$", "");
            return VerseParaStyles.Contains(style);
        }

        private class ParseContext
        {
            private readonly List<UsxToken> _tokens = new List<UsxToken>();

            public string Chapter { get; set; }
            public string Verse { get; set; }
            public bool IsInVerse => Chapter != null && Verse != null;
            public bool IsSentenceStart { get; set; } = true;
            public XElement ParentElement { get; set; }

            public void AddToken(string text, XElement elem = null)
            {
                _tokens.Add(new UsxToken(ParentElement, text, elem));
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
