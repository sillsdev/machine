using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SIL.Machine.Corpora
{
	public class UsfmStylesheet
	{
		private static readonly Dictionary<string, UsfmJustification> JustificationMappings = new Dictionary<string, UsfmJustification>(StringComparer.OrdinalIgnoreCase)
		{
			{"left", UsfmJustification.Left},
			{"center", UsfmJustification.Center},
			{"right", UsfmJustification.Right},
			{"both", UsfmJustification.Both}
		};

		private static readonly Dictionary<string, UsfmStyleType> StyleMappings = new Dictionary<string, UsfmStyleType>(StringComparer.OrdinalIgnoreCase)
		{
			{"character", UsfmStyleType.Character},
			{"paragraph", UsfmStyleType.Paragraph},
			{"note", UsfmStyleType.Note}
		};

		private static readonly Dictionary<string, UsfmTextType> TextTypeMappings = new Dictionary<string, UsfmTextType>(StringComparer.OrdinalIgnoreCase)
		{
			{"title", UsfmTextType.Title},
			{"section", UsfmTextType.Section},
			{"versetext", UsfmTextType.VerseText},
			{"notetext", UsfmTextType.NoteText},
			{"other", UsfmTextType.Other},
			{"backtranslation", UsfmTextType.BackTranslation},
			{"translationnote", UsfmTextType.TranslationNote},
			{"versenumber", UsfmTextType.VerseText},
			{"chapternumber", UsfmTextType.Other}
		};

		private static readonly Dictionary<string, UsfmTextProperties> TextPropertyMappings = new Dictionary<string, UsfmTextProperties>(StringComparer.OrdinalIgnoreCase)
		{
			{"verse", UsfmTextProperties.Verse},
			{"chapter", UsfmTextProperties.Chapter},
			{"paragraph", UsfmTextProperties.Paragraph},
			{"publishable", UsfmTextProperties.Publishable},
			{"vernacular", UsfmTextProperties.Vernacular},
			{"poetic", UsfmTextProperties.Poetic},
			{"level_1", UsfmTextProperties.Level1},
			{"level_2", UsfmTextProperties.Level2},
			{"level_3", UsfmTextProperties.Level3},
			{"level_4", UsfmTextProperties.Level4},
			{"level_5", UsfmTextProperties.Level5},
			{"crossreference", UsfmTextProperties.CrossReference},
			{"nonpublishable", UsfmTextProperties.Nonpublishable},
			{"nonvernacular", UsfmTextProperties.Nonvernacular},
			{"book", UsfmTextProperties.Book},
			{"note", UsfmTextProperties.Note}
		};

		private readonly Dictionary<string, UsfmMarker> _markers;

		public UsfmStylesheet(string stylesheetFileName, string alternateStylesheetFileName = null)
		{
			_markers = new Dictionary<string, UsfmMarker>();
			Parse(stylesheetFileName);
			if (!string.IsNullOrEmpty(alternateStylesheetFileName))
				Parse(alternateStylesheetFileName);
		}

		public UsfmMarker GetMarker(string markerStr)
		{
			UsfmMarker marker;
			if (!_markers.TryGetValue(markerStr, out marker))
			{
				marker = CreateMarker(markerStr);
				marker.StyleType = UsfmStyleType.Unknown;
			}
			return marker;
		}

		private static IEnumerable<string> GetEmbeddedStylesheet(string fileName)
		{
			using (var reader = new StreamReader(Assembly.GetExecutingAssembly()
				.GetManifestResourceStream("SIL.Machine.Corpora." + fileName)))
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

				UsfmMarker marker = CreateMarker(entry.Text);
				UsfmMarker endMarker = ParseMarkerEntry(marker, entries, i + 1);

				if (endMarker != null && !_markers.ContainsKey(endMarker.Marker))
					_markers[endMarker.Marker] = endMarker;

				foundStyles.Add(entry.Text);
			}
		}

		private UsfmMarker CreateMarker(string markerStr)
		{
			// If tag already exists update with addtl info (normally from custom.sty)
			UsfmMarker marker;
			if (!_markers.TryGetValue(markerStr, out marker))
			{
				marker = new UsfmMarker(markerStr);
				if (markerStr != "c" && markerStr != "v")
					marker.TextProperties = UsfmTextProperties.Publishable;
				_markers[markerStr] = marker;
			}

			return marker;
		}

		private static UsfmMarker ParseMarkerEntry(UsfmMarker marker, List<StylesheetEntry> stylesheetEntries, int entryIndex)
		{
			// The following items are present for conformance with
			// Paratext release 5.0 stylesheets.  Release 6.0 and later
			// follows the guidelines set in InitPropertyMaps.
			// Make sure \id gets book property
			if (marker.Marker == "id")
				marker.TextProperties |= UsfmTextProperties.Book;

			UsfmMarker endMarker = null;
			while (entryIndex < stylesheetEntries.Count)
			{
				StylesheetEntry entry = stylesheetEntries[entryIndex];
				++entryIndex;

				if (entry.Marker == "marker")
					break;

				switch (entry.Marker)
				{
					case "name":
						marker.Name = entry.Text;
						break;

					case "description":
						marker.Description = entry.Text;
						break;

					case "fontname":
						marker.FontName = entry.Text;
						break;

					case "fontsize":
						if (entry.Text == "-")
						{
							marker.FontSize = 0;
						}
						else
						{
							int fontSize;
							if (ParseInteger(entry, out fontSize))
								marker.FontSize = fontSize;
						}
						break;

					case "xmltag":
						marker.XmlTag = entry.Text;
						break;

					case "encoding":
						marker.Encoding = entry.Text;
						break;

					case "linespacing":
						int lineSpacing;
						if (ParseInteger(entry, out lineSpacing))
							marker.LineSpacing = lineSpacing;
						break;

					case "spacebefore":
						int spaceBefore;
						if (ParseInteger(entry, out spaceBefore))
							marker.SpaceBefore = spaceBefore;
						break;

					case "spaceafter":
						int spaceAfter;
						if (ParseInteger(entry, out spaceAfter))
							marker.SpaceAfter = spaceAfter;
						break;

					case "leftmargin":
						int leftMargin;
						if (ParseInteger(entry, out leftMargin))
							marker.LeftMargin = leftMargin;
						break;

					case "rightmargin":
						int rightMargin;
						if (ParseInteger(entry, out rightMargin))
							marker.RightMargin = rightMargin;
						break;

					case "firstlineindent":
						int firstLineIndent;
						if (ParseFloat(entry, out firstLineIndent))
							marker.FirstLineIndent = firstLineIndent;
						break;

					case "rank":
						if (entry.Text == "-")
						{
							marker.Rank = 0;
						}
						else
						{
							int rank;
							if (ParseInteger(entry, out rank))
								marker.Rank = rank;
						}
						break;

					case "bold":
						marker.Bold = entry.Text != "-";
						break;

					case "smallcaps":
						marker.SmallCaps = entry.Text != "-";
						break;

					case "subscript":
						marker.Subscript = entry.Text != "-";
						break;

					case "italic":
						marker.Italic = entry.Text != "-";
						break;

					case "regular":
						marker.Italic = marker.Bold = marker.Superscript = false;
						marker.Regular = true;
						break;

					case "underline":
						marker.Underline = entry.Text != "-";
						break;

					case "superscript":
						marker.Superscript = entry.Text != "-";
						break;

					case "testylename":
						break; // Ignore this tag, later we will use it to tie to FW styles

					case "notrepeatable":
						marker.NotRepeatable = entry.Text != "-";
						break;

					case "textproperties":
						ParseTextProperties(marker, entry);
						break;

					case "texttype":
						ParseTextType(marker, entry);
						break;

					case "color":
						if (entry.Text == "-")
						{
							marker.Color = 0;
						}
						else
						{
							int color;
							if (ParseInteger(entry, out color))
								marker.Color = color;
						}
						break;

					case "justification":
						UsfmJustification justification;
						if (JustificationMappings.TryGetValue(entry.Text, out justification))
							marker.Justification = justification;
						break;

					case "styletype":
						UsfmStyleType styleType;
						if (StyleMappings.TryGetValue(entry.Text, out styleType))
							marker.StyleType = styleType;
						break;

					case "occursunder":
						marker.OccursUnder.UnionWith(entry.Text.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries));
						break;

					case "endmarker":
						endMarker = MakeEndMarker(entry.Text);
						marker.EndMarker = entry.Text;
						break;
				}
			}

			// If we have not seen an end marker but this is a character style
			if (marker.StyleType == UsfmStyleType.Character && endMarker == null)
			{
				string endMarkerStr = marker.Marker + "*";
				endMarker = MakeEndMarker(endMarkerStr);
				marker.EndMarker = endMarkerStr;
			}

			// Special cases
			if (marker.TextType == UsfmTextType.Other
				&& (marker.TextProperties & UsfmTextProperties.Nonpublishable) == 0
				&& (marker.TextProperties & UsfmTextProperties.Chapter) == 0
				&& (marker.TextProperties & UsfmTextProperties.Verse) == 0
				&& (marker.StyleType == UsfmStyleType.Character || marker.StyleType == UsfmStyleType.Paragraph))
			{
				marker.TextProperties |= UsfmTextProperties.Publishable;
			}

			return endMarker;
		}

		private static UsfmMarker MakeEndMarker(string marker)
		{
			return new UsfmMarker(marker) {StyleType = UsfmStyleType.End};
		}

		private static bool ParseInteger(StylesheetEntry entry, out int result)
		{
			return int.TryParse(entry.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result >= 0;
		}

		private static bool ParseFloat(StylesheetEntry entry, out int result)
		{
			float floatResult;
			if (float.TryParse(entry.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out floatResult))
			{
				result = (int) (floatResult * 1000);
				return true;
			}

			result = 0;
			return false;
		}

		private static void ParseTextType(UsfmMarker qTag, StylesheetEntry entry)
		{
			if (entry.Text.Equals("chapternumber", StringComparison.OrdinalIgnoreCase))
				qTag.TextProperties |= UsfmTextProperties.Chapter;
			if (entry.Text.Equals("versenumber", StringComparison.CurrentCultureIgnoreCase))
				qTag.TextProperties |= UsfmTextProperties.Verse;

			UsfmTextType textType;
			if (TextTypeMappings.TryGetValue(entry.Text, out textType))
				qTag.TextType = textType;
		}

		private static void ParseTextProperties(UsfmMarker qTag, StylesheetEntry entry)
		{
			string text = entry.Text.ToLowerInvariant();
			string[] parts = text.Split();

			foreach (string part in parts)
			{
				if (part.Trim() == "")
					continue;

				UsfmTextProperties textProperty;
				if (TextPropertyMappings.TryGetValue(part, out textProperty))
					qTag.TextProperties |= textProperty;
			}

			if ((qTag.TextProperties & UsfmTextProperties.Nonpublishable) > 0)
				qTag.TextProperties &= ~UsfmTextProperties.Publishable;
		}

		private List<StylesheetEntry> SplitStylesheet(IEnumerable<string> fileLines)
		{
			List<StylesheetEntry> entries = new List<StylesheetEntry>();
			foreach (string line in fileLines.Select(l => l.Split('#')[0].Trim()))
			{
				if (line == "")
					continue;

				if (!line.StartsWith("\\", StringComparison.Ordinal))
				{
					// ignore lines that do not start with a backslash
					continue;
				}

				string[] parts = line.Split(new[] { ' ' }, 2);
				entries.Add(new StylesheetEntry(parts[0].Substring(1).ToLowerInvariant(),
					parts.Length > 1 ? parts[1].Trim() : ""));
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
