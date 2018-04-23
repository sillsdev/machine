using System.Text;

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
		Unknown
	}

	public class UsfmToken
	{
		public UsfmToken(string text)
		{
			Type = UsfmTokenType.Text;
			Text = text;
		}

		public UsfmToken(UsfmTokenType type, UsfmMarker marker, string text)
		{
			Type = type;
			Marker = marker;
			Text = text;
		}

		public UsfmTokenType Type { get; }

		public UsfmMarker Marker { get; }

		public string Text { get; }

		public override string ToString()
		{
			var sb = new StringBuilder();
			if (Marker != null)
				sb.Append(Marker);
			if (!string.IsNullOrEmpty(Text))
			{
				if (sb.Length > 0)
					sb.Append(" ");
				sb.Append(Text);
			}

			return sb.ToString();
		}
	}
}
