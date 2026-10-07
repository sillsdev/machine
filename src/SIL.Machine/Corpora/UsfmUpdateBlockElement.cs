using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public enum UsfmUpdateBlockElementType
    {
        Text,
        Paragraph,
        Embed,
        Style,
        Other,
    }

    public class UsfmUpdateBlockElement
    {
        public UsfmUpdateBlockElementType Type { get; }
        public List<UsfmToken> Tokens { get; }
        public bool MarkedForRemoval { get; set; }

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

        public string GetText()
        {
            return string.Concat(Tokens.Select(t => t.ToUsfm()));
        }

        public bool IsPlaceable(UpdateUsfmMarkerBehavior paragraphBehavior, UpdateUsfmMarkerBehavior styleBehavior)
        {
            if (MarkedForRemoval)
                return false;
            if (Type == UsfmUpdateBlockElementType.Paragraph)
                return paragraphBehavior == UpdateUsfmMarkerBehavior.Preserve && Tokens.Count == 1;
            if (Type == UsfmUpdateBlockElementType.Style)
                return styleBehavior == UpdateUsfmMarkerBehavior.Preserve;
            return false;
        }
    }
}
