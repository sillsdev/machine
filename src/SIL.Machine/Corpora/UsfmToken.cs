using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora
{
	public enum UsfmTokenType
	{
		Book,
		Chapter,
		Verse,
		Text,
		Paragraph,
		Character,
		Note,
		End,
		Milestone,
		MilestoneEnd,
		Attribute,
		Unknown
	}

	public class UsfmToken
	{
		private const string FullAttributeStr = @"(?<name>[-\w]+)\s*\=\s*\""(?<value>.+?)\""\s*";
		private static readonly Regex AttributeRegex = new Regex(@"(" + FullAttributeStr + @"(\s*" + FullAttributeStr +
			@")*|(?<default>[^\\=|]*))", RegexOptions.Compiled);

		private string _defaultAttributeName;
		private NamedAttribute[] _attributes;
		private bool _isDefaultAttribute;

		public UsfmToken(string text)
		{
			Type = UsfmTokenType.Text;
			Text = text;
		}

		public UsfmToken(UsfmTokenType type, UsfmMarker marker, string text, string data = null, bool isNested = false,
			int colSpan = 0)
		{
			Type = type;
			Marker = marker;
			Text = text;
			Data = data;
			IsNested = isNested;
			ColSpan = colSpan;
		}

		public UsfmTokenType Type { get; }

		public UsfmMarker Marker { get; }

		public string Text { get; set; }

		public string Data { get; }

		public bool IsNested { get; }

		public int ColSpan { get; }

		public string GetAttribute(string name)
		{
			if (_attributes == null || _attributes.Length == 0)
				return "";

			NamedAttribute attribute = _attributes.FirstOrDefault(a => a.Name == name);
			return attribute?.Value ?? "";
		}

		public bool SetAttributes(string attributesValue, string defaultAttributeName, ref string adjustedText,
			bool preserveWhitespace = false)
		{
			if (string.IsNullOrEmpty(attributesValue))
				return false;


			// for figures, convert 2.0 format to 3.0 format. Will need to write this as the 2.0 format
			// if the project is not upgrated.
			if (Marker.Tag == "fig" && attributesValue.Count(c => c == '|') == 5)
			{
				List<NamedAttribute> attributeList = new List<NamedAttribute>(6);
				string[] parts = attributesValue.Split('|');
				AppendAttribute(attributeList, "alt", adjustedText);
				AppendAttribute(attributeList, "src", parts[0]);
				AppendAttribute(attributeList, "size", parts[1]);
				AppendAttribute(attributeList, "loc", parts[2]);
				AppendAttribute(attributeList, "copy", parts[3]);
				string whitespace = "";
				if (preserveWhitespace)
					whitespace = adjustedText.Substring(0, adjustedText.Length - adjustedText.TrimStart().Length);
				adjustedText = whitespace + parts[4];
				AppendAttribute(attributeList, "ref", parts[5]);
				_attributes = attributeList.ToArray();
				return true;
			}

			Match match = AttributeRegex.Match(attributesValue);
			if (!match.Success || match.Length != attributesValue.Length)
				return false; // must match entire string

			Group defaultValue = match.Groups["default"];
			if (defaultValue.Success)
			{
				// only accept default value it there is a defined default attribute
				if (defaultAttributeName != null)
				{
					_attributes = new[] { new NamedAttribute(defaultAttributeName, defaultValue.Value) };
					_defaultAttributeName = defaultAttributeName;
					_isDefaultAttribute = true;
					return true;
				}
				return false;
			}

			CaptureCollection attributeNames = match.Groups["name"].Captures;
			CaptureCollection attributeValues = match.Groups["value"].Captures;
			if (attributeNames.Count == 0 || attributeNames.Count != attributeValues.Count)
				return false;

			_defaultAttributeName = defaultAttributeName;
			NamedAttribute[] attributes = new NamedAttribute[attributeNames.Count];
			for (int i = 0; i < attributeNames.Count; i++)
				attributes[i] = new NamedAttribute(attributeNames[i].Value, attributeValues[i].Value, attributeValues[i].Index);
			_attributes = attributes;
			return true;
		}

		public void CopyAttributes(UsfmToken sourceToken)
		{
			_attributes = sourceToken._attributes;
			_defaultAttributeName = sourceToken._defaultAttributeName;
		}

		private static void AppendAttribute(List<NamedAttribute> attributes, string name, string value)
		{
			value = value?.Trim();  // don't want to have attribute that is just spaces
			if (!string.IsNullOrEmpty(value))
				attributes.Add(new NamedAttribute(name, value));
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			if (Marker != null)
			{
				sb.Append(IsNested ? $"\\+{Marker.Tag}" : Marker.ToString());
				if (ColSpan >= 2)
				{
					int col = int.Parse(Marker.Tag[Marker.Tag.Length - 1].ToString(), CultureInfo.InvariantCulture);
					sb.Append($"-{col + ColSpan - 1}");
				}
			}
			if (!string.IsNullOrEmpty(Text))
			{
				if (sb.Length > 0)
					sb.Append(" ");
				sb.Append(Text);
			}

			if (Type == UsfmTokenType.Attribute)
			{
				if (_attributes != null && _attributes.Length > 0)
				{
					sb.Append("|");
					if (_isDefaultAttribute && _attributes.Length == 1)
						sb.Append(_attributes[0].Value);
					else
						sb.Append(string.Join(" ", _attributes.Select(a => a.ToString())));
				}
			}
			else if (!string.IsNullOrEmpty(Data))
			{
				if (sb.Length > 0)
					sb.Append(" ");
				sb.Append(Data);
			}

			return sb.ToString();
		}

		private sealed class NamedAttribute
		{
			public NamedAttribute(string name, string value, int offset = 0)
			{
				Name = name;
				Value = value;
				Offset = offset;
			}

			public string Name { get; }
			public string Value { get; }

			public int Offset { get; }

			public override string ToString()
			{
				return Name + $"=\"{Value}\"";
			}
		}
	}
}
