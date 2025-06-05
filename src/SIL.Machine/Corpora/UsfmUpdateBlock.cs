using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class UsfmUpdateBlock
    {
        public IReadOnlyList<ScriptureRef> Refs
        {
            get => _refs;
        }
        public IReadOnlyList<UsfmUpdateBlockElement> Elements
        {
            get => _elements;
        }

        private readonly List<ScriptureRef> _refs;
        private readonly List<UsfmUpdateBlockElement> _elements;

        public UsfmUpdateBlock(
            IEnumerable<ScriptureRef> refs = null,
            IEnumerable<UsfmUpdateBlockElement> elements = null
        )
        {
            _refs = refs != null ? refs.ToList() : new List<ScriptureRef>();
            _elements = elements != null ? elements.ToList() : new List<UsfmUpdateBlockElement>();
        }

        public void AddText(IEnumerable<UsfmToken> tokens)
        {
            _elements.Add(new UsfmUpdateBlockElement(UsfmUpdateBlockElementType.Text, tokens.ToList()));
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
            _elements.Add(new UsfmUpdateBlockElement(type, new List<UsfmToken> { token }, markedForRemoval));
        }

        public void AddEmbed(IEnumerable<UsfmToken> tokens, bool markedForRemoval = false)
        {
            _elements.Add(
                new UsfmUpdateBlockElement(UsfmUpdateBlockElementType.Embed, tokens.ToList(), markedForRemoval)
            );
        }

        public void ExtendLastElement(IEnumerable<UsfmToken> tokens)
        {
            _elements.Last().Tokens.AddRange(tokens);
        }

        public void UpdateRefs(IEnumerable<ScriptureRef> refs)
        {
            _refs.Clear();
            _refs.AddRange(refs);
        }

        public UsfmUpdateBlockElement GetLastParagraph()
        {
            for (int i = _elements.Count - 1; i >= 0; i--)
            {
                UsfmUpdateBlockElement element = _elements[i];
                if (element.Type == UsfmUpdateBlockElementType.Paragraph)
                    return element;
            }
            return null;
        }

        public UsfmUpdateBlockElement Pop()
        {
            UsfmUpdateBlockElement element = _elements.Last();
            _elements.RemoveAt(_elements.Count - 1);
            return element;
        }

        public List<UsfmToken> GetTokens()
        {
            return _elements.SelectMany(e => e.GetTokens()).ToList();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UsfmUpdateBlock))
                return false;

            UsfmUpdateBlock other = (UsfmUpdateBlock)obj;

            return _refs.SequenceEqual(other._refs) && _elements.SequenceEqual(other._elements);
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 31 + _refs.GetHashCode();
            hash = hash * 31 + _elements.GetHashCode();
            return hash;
        }

        public UsfmUpdateBlock Clone()
        {
            return new UsfmUpdateBlock(_refs, _elements);
        }
    }
}
