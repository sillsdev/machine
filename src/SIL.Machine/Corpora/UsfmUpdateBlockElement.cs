using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    public enum UsfmUpdateBlockElementType
    {
        Text,
        Paragraph,
        Embed,
        Style,
        Other
    }

    public class UsfmUpdateBlockElement
    {
        public UsfmUpdateBlockElementType Type { get; }
        public List<UsfmToken> Tokens { get; }
        public bool MarkedForRemoval { get; }

        public UsfmUpdateBlockElement(
            UsfmUpdateBlockElementType type,
            List<UsfmToken> tokens,
            bool markedForRemoval = false
        )
        {
            Type = type;
            Tokens = tokens;
            MarkedForRemoval = markedForRemoval;
        }

        public List<UsfmToken> GetTokens()
        {
            return MarkedForRemoval ? new List<UsfmToken>() : new List<UsfmToken>(Tokens);
        }
    }
}
