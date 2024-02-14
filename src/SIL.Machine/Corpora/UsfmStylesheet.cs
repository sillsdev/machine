using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora
{
    public class UsfmStylesheet
    {
        private static readonly Regex s_cellRangeRegex = new Regex(
            @"^(t[ch][cr]?[1-5])-([2-5])$",
            RegexOptions.Compiled
        );

        private static readonly Dictionary<string, UsfmJustification> s_justificationMappings = new Dictionary<
            string,
            UsfmJustification
        >(StringComparer.OrdinalIgnoreCase)
        {
            { "left", UsfmJustification.Left },
            { "center", UsfmJustification.Center },
            { "right", UsfmJustification.Right },
            { "both", UsfmJustification.Both }
        };

        private static readonly Dictionary<string, UsfmStyleType> s_styleMappings = new Dictionary<
            string,
            UsfmStyleType
        >(StringComparer.OrdinalIgnoreCase)
        {
            { "character", UsfmStyleType.Character },
            { "paragraph", UsfmStyleType.Paragraph },
            { "note", UsfmStyleType.Note },
            { "milestone", UsfmStyleType.Milestone }
        };

        private static readonly Dictionary<string, UsfmTextType> s_textTypeMappings = new Dictionary<
            string,
            UsfmTextType
        >(StringComparer.OrdinalIgnoreCase)
        {
            { "title", UsfmTextType.Title },
            { "section", UsfmTextType.Section },
            { "versetext", UsfmTextType.VerseText },
            { "notetext", UsfmTextType.NoteText },
            { "other", UsfmTextType.Other },
            { "backtranslation", UsfmTextType.BackTranslation },
            { "translationnote", UsfmTextType.TranslationNote },
            { "versenumber", UsfmTextType.VerseText },
            { "chapternumber", UsfmTextType.Other }
        };

        private static readonly Dictionary<string, UsfmTextProperties> s_textPropertyMappings = new Dictionary<
            string,
            UsfmTextProperties
        >(StringComparer.OrdinalIgnoreCase)
        {
            { "verse", UsfmTextProperties.Verse },
            { "chapter", UsfmTextProperties.Chapter },
            { "paragraph", UsfmTextProperties.Paragraph },
            { "publishable", UsfmTextProperties.Publishable },
            { "vernacular", UsfmTextProperties.Vernacular },
            { "poetic", UsfmTextProperties.Poetic },
            { "level_1", UsfmTextProperties.Level1 },
            { "level_2", UsfmTextProperties.Level2 },
            { "level_3", UsfmTextProperties.Level3 },
            { "level_4", UsfmTextProperties.Level4 },
            { "level_5", UsfmTextProperties.Level5 },
            { "crossreference", UsfmTextProperties.CrossReference },
            { "nonpublishable", UsfmTextProperties.Nonpublishable },
            { "nonvernacular", UsfmTextProperties.Nonvernacular },
            { "book", UsfmTextProperties.Book },
            { "note", UsfmTextProperties.Note }
        };

        private readonly Dictionary<string, UsfmTag> _markers;

        public UsfmStylesheet(string stylesheetFileName, string alternateStylesheetFileName = null)
        {
            _markers = new Dictionary<string, UsfmTag>();
            Parse(stylesheetFileName);
            if (!string.IsNullOrEmpty(alternateStylesheetFileName))
                Parse(alternateStylesheetFileName);
        }

        public UsfmTag GetTag(string marker)
        {
            if (_markers.TryGetValue(marker, out UsfmTag tag))
                return tag;

            if (IsCellRange(marker, out string baseMarker, out _) && _markers.TryGetValue(baseMarker, out tag))
                return tag;

            tag = CreateTag(marker);
            tag.StyleType = UsfmStyleType.Unknown;
            return tag;
        }

        public static bool IsCellRange(string tag, out string baseMarker, out int colSpan)
        {
            var match = s_cellRangeRegex.Match(tag);
            if (match.Success)
            {
                baseMarker = match.Groups[1].Value;
                colSpan = match.Groups[2].Value[0] - baseMarker[baseMarker.Length - 1] + 1;
                if (colSpan >= 2)
                    return true;
            }

            baseMarker = tag;
            colSpan = 0;
            return false;
        }

        private static IEnumerable<string> GetEmbeddedStylesheet(string fileName)
        {
            using (
                var reader = new StreamReader(
                    Assembly.GetExecutingAssembly().GetManifestResourceStream("SIL.Machine.Corpora." + fileName)
                )
            )
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    yield return line;
            }
        }

        private void Parse(string stylesheetFileName)
        {
            IEnumerable<string> lines;
            if (File.Exists(stylesheetFileName))
            {
                lines = File.ReadAllLines(stylesheetFileName);
            }
            else
            {
                string fileName = Path.GetFileName(stylesheetFileName);
                if (fileName == "usfm.sty" || fileName == "usfm_sb.sty")
                    lines = GetEmbeddedStylesheet(fileName);
                else
                    throw new ArgumentException("The stylesheet does not exist.", nameof(stylesheetFileName));
            }

            List<StylesheetEntry> entries = SplitStylesheet(lines);

            HashSet<string> foundStyles = new HashSet<string>();
            for (int i = 0; i < entries.Count; ++i)
            {
                StylesheetEntry entry = entries[i];

                if (entry.Marker != "marker")
                    continue;

                string[] parts = entry.Text.Split();
                if (parts.Length > 1 && parts[1] == "-")
                {
                    // If the entry looks like "\marker xy -" remove the tag and its end tag if any
                    _markers.Remove(parts[0]);
                    _markers.Remove(parts[0] + "*");
                    continue;
                }

                UsfmTag marker = CreateTag(entry.Text);
                UsfmTag endMarker = ParseTagEntry(marker, entries, i + 1);

                if (endMarker != null && !_markers.ContainsKey(endMarker.Marker))
                    _markers[endMarker.Marker] = endMarker;

                foundStyles.Add(entry.Text);
            }
        }

        private UsfmTag CreateTag(string marker)
        {
            // If tag already exists update with addtl info (normally from custom.sty)
            if (!_markers.TryGetValue(marker, out UsfmTag tag))
            {
                tag = new UsfmTag(marker);
                if (marker != "c" && marker != "v")
                    tag.TextProperties = UsfmTextProperties.Publishable;
                _markers[marker] = tag;
            }

            return tag;
        }

        private static UsfmTag ParseTagEntry(UsfmTag tag, List<StylesheetEntry> stylesheetEntries, int entryIndex)
        {
            // The following items are present for conformance with
            // Paratext release 5.0 stylesheets.  Release 6.0 and later
            // follows the guidelines set in InitPropertyMaps.
            // Make sure \id gets book property
            if (tag.Marker == "id")
                tag.TextProperties |= UsfmTextProperties.Book;

            UsfmTag endTag = null;
            while (entryIndex < stylesheetEntries.Count)
            {
                StylesheetEntry entry = stylesheetEntries[entryIndex];
                ++entryIndex;

                if (entry.Marker == "marker")
                    break;

                switch (entry.Marker)
                {
                    case "name":
                        tag.Name = entry.Text;
                        break;

                    case "description":
                        tag.Description = entry.Text;
                        break;

                    case "fontname":
                        tag.FontName = entry.Text;
                        break;

                    case "fontsize":
                        if (entry.Text == "-")
                        {
                            tag.FontSize = 0;
                        }
                        else
                        {
                            if (ParseInteger(entry, out int fontSize))
                                tag.FontSize = fontSize;
                        }
                        break;

                    case "xmltag":
                        tag.XmlTag = entry.Text;
                        break;

                    case "encoding":
                        tag.Encoding = entry.Text;
                        break;

                    case "linespacing":
                        int lineSpacing;
                        if (ParseInteger(entry, out lineSpacing))
                            tag.LineSpacing = lineSpacing;
                        break;

                    case "spacebefore":
                        int spaceBefore;
                        if (ParseInteger(entry, out spaceBefore))
                            tag.SpaceBefore = spaceBefore;
                        break;

                    case "spaceafter":
                        int spaceAfter;
                        if (ParseInteger(entry, out spaceAfter))
                            tag.SpaceAfter = spaceAfter;
                        break;

                    case "leftmargin":
                        int leftMargin;
                        if (ParseInteger(entry, out leftMargin))
                            tag.LeftMargin = leftMargin;
                        break;

                    case "rightmargin":
                        int rightMargin;
                        if (ParseInteger(entry, out rightMargin))
                            tag.RightMargin = rightMargin;
                        break;

                    case "firstlineindent":
                        int firstLineIndent;
                        if (ParseFloat(entry, out firstLineIndent))
                            tag.FirstLineIndent = firstLineIndent;
                        break;

                    case "rank":
                        if (entry.Text == "-")
                        {
                            tag.Rank = 0;
                        }
                        else
                        {
                            if (ParseInteger(entry, out int rank))
                                tag.Rank = rank;
                        }
                        break;

                    case "bold":
                        tag.Bold = entry.Text != "-";
                        break;

                    case "smallcaps":
                        tag.SmallCaps = entry.Text != "-";
                        break;

                    case "subscript":
                        tag.Subscript = entry.Text != "-";
                        break;

                    case "italic":
                        tag.Italic = entry.Text != "-";
                        break;

                    case "regular":
                        tag.Italic = tag.Bold = tag.Superscript = false;
                        tag.Regular = true;
                        break;

                    case "underline":
                        tag.Underline = entry.Text != "-";
                        break;

                    case "superscript":
                        tag.Superscript = entry.Text != "-";
                        break;

                    case "testylename":
                        break; // Ignore this tag, later we will use it to tie to FW styles

                    case "notrepeatable":
                        tag.NotRepeatable = entry.Text != "-";
                        break;

                    case "textproperties":
                        ParseTextProperties(tag, entry);
                        break;

                    case "texttype":
                        ParseTextType(tag, entry);
                        break;

                    case "color":
                        if (entry.Text == "-")
                        {
                            tag.Color = 0;
                        }
                        else
                        {
                            if (ParseInteger(entry, out int color))
                                tag.Color = color;
                        }
                        break;

                    case "justification":
                        UsfmJustification justification;
                        if (s_justificationMappings.TryGetValue(entry.Text, out justification))
                            tag.Justification = justification;
                        break;

                    case "styletype":
                        UsfmStyleType styleType;
                        if (s_styleMappings.TryGetValue(entry.Text, out styleType))
                            tag.StyleType = styleType;
                        break;

                    case "occursunder":
                        tag.OccursUnder.UnionWith(
                            entry.Text.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                        );
                        break;

                    case "endmarker":
                        endTag = MakeEndTag(entry.Text);
                        tag.EndMarker = entry.Text;
                        break;

                    case "attributes":
                        ParseAttributes(tag, entry);
                        break;
                }
            }

            // If we have not seen an end marker but this is a character style
            if (tag.StyleType == UsfmStyleType.Character && endTag == null)
            {
                string endMarkerStr = tag.Marker + "*";
                endTag = MakeEndTag(endMarkerStr);
                tag.EndMarker = endMarkerStr;
            }
            else if (tag.StyleType == UsfmStyleType.Milestone)
            {
                if (endTag != null)
                {
                    endTag.StyleType = UsfmStyleType.MilestoneEnd;
                    // eid is always an optional attribute for the end marker
                    tag.Attributes.Add(new UsfmStyleAttribute("eid", isRequired: false));
                    endTag.Name = tag.Name;
                }
            }

            // Special cases
            if (
                tag.TextType == UsfmTextType.Other
                && (tag.TextProperties & UsfmTextProperties.Nonpublishable) == 0
                && (tag.TextProperties & UsfmTextProperties.Chapter) == 0
                && (tag.TextProperties & UsfmTextProperties.Verse) == 0
                && (tag.StyleType == UsfmStyleType.Character || tag.StyleType == UsfmStyleType.Paragraph)
            )
            {
                tag.TextProperties |= UsfmTextProperties.Publishable;
            }

            return endTag;
        }

        private static UsfmTag MakeEndTag(string marker)
        {
            return new UsfmTag(marker) { StyleType = UsfmStyleType.End };
        }

        private static bool ParseInteger(StylesheetEntry entry, out int result)
        {
            return int.TryParse(entry.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result)
                && result >= 0;
        }

        private static bool ParseFloat(StylesheetEntry entry, out int result)
        {
            float floatResult;
            if (
                float.TryParse(
                    entry.Text,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out floatResult
                )
            )
            {
                result = (int)(floatResult * 1000);
                return true;
            }

            result = 0;
            return false;
        }

        private static void ParseTextType(UsfmTag tag, StylesheetEntry entry)
        {
            if (entry.Text.Equals("chapternumber", StringComparison.OrdinalIgnoreCase))
                tag.TextProperties |= UsfmTextProperties.Chapter;
            if (entry.Text.Equals("versenumber", StringComparison.CurrentCultureIgnoreCase))
                tag.TextProperties |= UsfmTextProperties.Verse;

            UsfmTextType textType;
            if (s_textTypeMappings.TryGetValue(entry.Text, out textType))
                tag.TextType = textType;
        }

        private static void ParseTextProperties(UsfmTag tag, StylesheetEntry entry)
        {
            string text = entry.Text.ToLowerInvariant();
            string[] parts = text.Split();

            foreach (string part in parts)
            {
                if (part.Trim() == "")
                    continue;

                UsfmTextProperties textProperty;
                if (s_textPropertyMappings.TryGetValue(part, out textProperty))
                    tag.TextProperties |= textProperty;
            }

            if ((tag.TextProperties & UsfmTextProperties.Nonpublishable) > 0)
                tag.TextProperties &= ~UsfmTextProperties.Publishable;
        }

        private static void ParseAttributes(UsfmTag tag, StylesheetEntry entry)
        {
            string[] attributeNames = entry.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (attributeNames.Length == 0)
                throw new ArgumentException("Attributes cannot be empty.");
            bool foundOptional = false;
            foreach (string attribute in attributeNames)
            {
                bool isOptional = attribute.StartsWith("?");
                if (!isOptional && foundOptional)
                    throw new ArgumentException("Required attributes must precede optional attributes.");

                tag.Attributes.Add(
                    new UsfmStyleAttribute(isOptional ? attribute.Substring(1) : attribute, !isOptional)
                );
                foundOptional |= isOptional;
            }

            tag.DefaultAttributeName = tag.Attributes.Count(a => a.IsRequired) <= 1 ? tag.Attributes[0].Name : null;
        }

        private List<StylesheetEntry> SplitStylesheet(IEnumerable<string> fileLines)
        {
            List<StylesheetEntry> entries = new List<StylesheetEntry>();
            // Lines that are not compatible with USFM 2 are started with #!, so these two characters are stripped from
            // the beginning of lines.
            foreach (
                string line in fileLines
                    .Select(l => l.StartsWith("#!", StringComparison.Ordinal) ? l.Substring(2) : l)
                    .Select(l => l.Split('#')[0].Trim())
            )
            {
                if (line == "")
                    continue;

                if (!line.StartsWith("\\", StringComparison.Ordinal))
                {
                    // ignore lines that do not start with a backslash
                    continue;
                }

                string[] parts = line.Split(new[] { ' ' }, 2);
                entries.Add(
                    new StylesheetEntry(
                        parts[0].Substring(1).ToLowerInvariant(),
                        parts.Length > 1 ? parts[1].Trim() : ""
                    )
                );
            }

            return entries;
        }

        private class StylesheetEntry
        {
            public StylesheetEntry(string marker, string text)
            {
                Marker = marker;
                Text = text;
            }

            public string Marker { get; }
            public string Text { get; }
        }
    }
}
