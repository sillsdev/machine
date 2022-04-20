using System;
using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public enum UsfmTextType
	{
		Title,
		Section,
		VerseText,
		NoteText,
		Other,
		BackTranslation,
		TranslationNote
	}

	public enum UsfmJustification
	{
		Left,
		Center,
		Right,
		Both
	}

	public enum UsfmStyleType
	{
		Unknown,
		Character,
		Note,
		Paragraph,
		End,
		Milestone,
		MilestoneEnd
	}

	[Flags]
	public enum UsfmTextProperties
	{
		None = 0x0,
		Verse = 0x1,
		Chapter = 0x2,
		Paragraph = 0x4,
		Publishable = 0x8,
		Vernacular = 0x10,
		Poetic = 0x20,
		OtherTextBegin = 0x40,
		OtherTextEnd = 0x80,
		Level1 = 0x100,
		Level2 = 0x200,
		Level3 = 0x400,
		Level4 = 0x800,
		Level5 = 0x1000,
		CrossReference = 0x2000,
		Nonpublishable = 0x4000,
		Nonvernacular = 0x8000,
		Book = 0x10000,
		Note = 0x20000
	}

	public sealed class UsfmStyleAttribute
	{
		public UsfmStyleAttribute(string name, bool isRequired)
		{
			Name = name;
			IsRequired = isRequired;
		}

		public string Name { get; }
		public bool IsRequired { get; }
	}

	public class UsfmTag
	{
		private readonly HashSet<string> _occursUnder;
		private readonly List<UsfmStyleAttribute> _attributes;

		public UsfmTag(string marker)
		{
			Marker = marker;
			_occursUnder = new HashSet<string>();
			_attributes = new List<UsfmStyleAttribute>();
		}

		public bool Bold { get; set; }

		public string Description { get; set; }

		public string Encoding { get; set; }

		public string EndMarker { get; set; }

		public int FirstLineIndent { get; set; }

		public string FontName { get; set; }

		public int FontSize { get; set; }

		public bool Italic { get; set; }

		public UsfmJustification Justification { get; set; }

		public int LeftMargin { get; set; }

		public int LineSpacing { get; set; }

		public string Marker { get; }

		public string Name { get; set; }

		public bool NotRepeatable { get; set; }

		public ISet<string> OccursUnder => _occursUnder;

		public int Rank { get; set; }

		public int RightMargin { get; set; }

		public bool SmallCaps { get; set; }

		public int SpaceAfter { get; set; }

		public int SpaceBefore { get; set; }

		public UsfmStyleType StyleType { get; set; }

		public bool Subscript { get; set; }

		public bool Superscript { get; set; }

		public UsfmTextProperties TextProperties { get; set; }

		public UsfmTextType TextType { get; set; }

		public bool Underline { get; set; }

		public string XmlTag { get; set; }

		public bool Regular { get; set; }

		public int Color { get; set; }

		public IList<UsfmStyleAttribute> Attributes => _attributes;

		public string DefaultAttributeName { get; set; }

		public override string ToString()
		{
			return string.Format("\\{0}", Marker);
		}
	}
}
