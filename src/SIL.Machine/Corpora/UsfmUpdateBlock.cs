using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class UsfmUpdateBlock
    {
        public List<ScriptureRef> Refs { get; }
        public List<UsfmUpdateBlockElement> Elements { get; }

        public UsfmUpdateBlock(
            IEnumerable<ScriptureRef> refs = null,
            IEnumerable<UsfmUpdateBlockElement> elements = null
        )
        {
            Refs = refs != null ? refs.ToList() : new List<ScriptureRef>();
            Elements = elements != null ? elements.ToList() : new List<UsfmUpdateBlockElement>();
        }

        public void AddText(IEnumerable<UsfmToken> tokens)
        {
            Elements.Add(new UsfmUpdateBlockElement(UsfmUpdateBlockElementType.Text, tokens.ToList()));
        }

        public void AddToken(UsfmToken token, bool markedForRemoval = false)
        {
            UsfmUpdateBlockElementType type;
            switch (token.Type)
            {
                case UsfmTokenType.Text:
                    type = UsfmUpdateBlockElementType.Text;
                    break;
                case UsfmTokenType.Paragraph:
                    type = UsfmUpdateBlockElementType.Paragraph;
                    break;
                case UsfmTokenType.Character:
                case UsfmTokenType.End:
                    type = UsfmUpdateBlockElementType.Style;
                    break;
                default:
                    type = UsfmUpdateBlockElementType.Other;
                    break;
            }
            Elements.Add(new UsfmUpdateBlockElement(type, new List<UsfmToken> { token }, markedForRemoval));
        }

        public void AddEmbed(IEnumerable<UsfmToken> tokens, bool markedForRemoval = false)
        {
            Elements.Add(
                new UsfmUpdateBlockElement(UsfmUpdateBlockElementType.Embed, tokens.ToList(), markedForRemoval)
            );
        }

        public void ExtendLastElement(IEnumerable<UsfmToken> tokens)
        {
            Elements.Last().Tokens.AddRange(tokens);
        }

        public void UpdateRefs(IEnumerable<ScriptureRef> refs)
        {
            Refs.Clear();
            Refs.AddRange(refs);
        }

        public List<UsfmToken> GetTokens()
        {
            return Elements.SelectMany(e => e.GetTokens()).ToList();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UsfmUpdateBlock))
                return false;

            UsfmUpdateBlock other = (UsfmUpdateBlock)obj;

            return Refs.SequenceEqual(other.Refs) && Elements.SequenceEqual(other.Elements);
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 31 + Refs.GetHashCode();
            hash = hash * 31 + Elements.GetHashCode();
            return hash;
        }

        public UsfmUpdateBlock Clone()
        {
            return new UsfmUpdateBlock(Refs, Elements);
        }
    }
}
