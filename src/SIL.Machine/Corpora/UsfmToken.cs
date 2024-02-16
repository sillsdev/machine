using System.Collections.Generic;
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
        private static readonly Regex AttributeRegex = new Regex(
            @"(" + FullAttributeStr + @"(\s*" + FullAttributeStr + @")*|(?<default>[^\\=|]*))",
            RegexOptions.Compiled
        );

        private string _defaultAttributeName;

        public UsfmToken(string text)
        {
            Type = UsfmTokenType.Text;
            Text = text;
        }

        public UsfmToken(UsfmTokenType type, string marker, string text, string endMarker, string data = null)
        {
            Type = type;
            Marker = marker;
            Text = text;
            Data = data;
            EndMarker = endMarker;
        }

        public UsfmTokenType Type { get; }

        public string Marker { get; }
        public string EndMarker { get; }

        public string Text { get; set; }

        public string Data { get; }

        public IReadOnlyList<UsfmAttribute> Attributes { get; private set; }

        public string NestlessMarker
        {
            get { return Marker != null && Marker[0] == '+' ? Marker.Substring(1) : Marker; }
        }

        public string GetAttribute(string name)
        {
            if (Attributes == null || Attributes.Count == 0)
                return "";

            UsfmAttribute attribute = Attributes.FirstOrDefault(a => a.Name == name);
            return attribute?.Value ?? "";
        }

        public bool SetAttributes(
            string attributesValue,
            string defaultAttributeName,
            ref string adjustedText,
            bool preserveWhitespace = false
        )
        {
            if (string.IsNullOrEmpty(attributesValue))
                return false;

            // for figures, convert 2.0 format to 3.0 format. Will need to write this as the 2.0 format
            // if the project is not upgraded.
            if (NestlessMarker == "fig" && attributesValue.Count(c => c == '|') == 6)
            {
                List<UsfmAttribute> attributeList = new List<UsfmAttribute>(6);
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
                Attributes = attributeList;
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
                    Attributes = new[] { new UsfmAttribute(defaultAttributeName, defaultValue.Value) };
                    _defaultAttributeName = defaultAttributeName;
                    return true;
                }
                return false;
            }

            CaptureCollection attributeNames = match.Groups["name"].Captures;
            CaptureCollection attributeValues = match.Groups["value"].Captures;
            if (attributeNames.Count == 0 || attributeNames.Count != attributeValues.Count)
                return false;

            _defaultAttributeName = defaultAttributeName;
            UsfmAttribute[] attributes = new UsfmAttribute[attributeNames.Count];
            for (int i = 0; i < attributeNames.Count; i++)
            {
                attributes[i] = new UsfmAttribute(
                    attributeNames[i].Value,
                    attributeValues[i].Value,
                    attributeValues[i].Index
                );
            }

            Attributes = attributes;
            return true;
        }

        public void CopyAttributes(UsfmToken sourceToken)
        {
            Attributes = sourceToken.Attributes;
            _defaultAttributeName = sourceToken._defaultAttributeName;
        }

        private static void AppendAttribute(List<UsfmAttribute> attributes, string name, string value)
        {
            value = value?.Trim(); // don't want to have attribute that is just spaces
            if (!string.IsNullOrEmpty(value))
                attributes.Add(new UsfmAttribute(name, value));
        }

        public int GetLength(bool includeNewlines = false, bool addSpaces = true)
        {
            // WARNING: This logic in this method needs to match the logic in ToUsfm()

            int totalLength = (Text != null) ? Text.Length : 0;
            if (Type == UsfmTokenType.Attribute)
            {
                totalLength += ToAttributeString().Length;
            }
            else if (Marker != null)
            {
                if (
                    includeNewlines
                    && (Type == UsfmTokenType.Paragraph || Type == UsfmTokenType.Chapter || Type == UsfmTokenType.Verse)
                )
                {
                    totalLength += 2;
                }
                totalLength += Marker.Length + 1; // marker and backslash
                if (addSpaces && (Marker.Length == 0 || Marker[Marker.Length - 1] != '*'))
                    totalLength++; // space

                if (!string.IsNullOrEmpty(Data))
                {
                    if (!addSpaces && (Marker.Length == 0 || Marker[Marker.Length - 1] != '*'))
                        totalLength++;
                    totalLength += Data.Length;
                    if (addSpaces)
                        totalLength++;
                }

                if (Type == UsfmTokenType.Milestone || Type == UsfmTokenType.MilestoneEnd)
                {
                    string attributes = ToAttributeString();
                    if (attributes != "")
                    {
                        totalLength += attributes.Length;
                    }
                    else
                    {
                        // remove space that was put after marker - not needed when there are no attributes.
                        totalLength--;
                    }

                    totalLength += 2; // End of the milestone
                }
            }
            return totalLength;
        }

        public string ToUsfm(bool includeNewlines = false, bool addSpaces = true)
        {
            // WARNING: The logic in this method needs to match the logic in GetLength()

            string toReturn = Text ?? "";
            if (Type == UsfmTokenType.Attribute)
            {
                toReturn += ToAttributeString();
            }
            else if (Marker != null)
            {
                StringBuilder sb = new StringBuilder();
                if (
                    includeNewlines
                    && (Type == UsfmTokenType.Paragraph || Type == UsfmTokenType.Chapter || Type == UsfmTokenType.Verse)
                )
                {
                    sb.Append("\r\n");
                }
                sb.Append('\\');
                if (Marker.Length > 0)
                    sb.Append(Marker);
                if (addSpaces && (Marker.Length == 0 || Marker[Marker.Length - 1] != '*'))
                    sb.Append(' ');

                if (!string.IsNullOrEmpty(Data))
                {
                    if (!addSpaces && (Marker.Length == 0 || Marker[Marker.Length - 1] != '*'))
                        sb.Append(' ');
                    sb.Append(Data);
                    if (addSpaces)
                        sb.Append(' ');
                }

                if (Type == UsfmTokenType.Milestone || Type == UsfmTokenType.MilestoneEnd)
                {
                    string attributes = ToAttributeString();
                    if (attributes != "")
                    {
                        sb.Append(attributes);
                    }
                    else
                    {
                        // remove space that was put after marker - not needed when there are no attributes.
                        sb.Length--;
                    }
                    sb.Append(@"\*");
                }
                toReturn += sb.ToString();
            }
            return toReturn;
        }

        public string ToAttributeString()
        {
            if (Attributes == null || Attributes.Count == 0)
                return "";

            if (!string.IsNullOrEmpty(Data))
                return "|" + Data;

            if (Attributes.Count == 1 && Attributes[0].Name == _defaultAttributeName)
                return "|" + Attributes[0].Value;

            return "|" + string.Join(" ", Attributes.Select(a => a.ToString()));
        }

        public override string ToString()
        {
            return ToUsfm(addSpaces: false);
        }
    }
}
