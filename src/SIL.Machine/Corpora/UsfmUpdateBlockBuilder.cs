using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    internal class UsfmUpdateBlockBuilder
    {
        private readonly List<ScriptureRef> _refs;
        private readonly List<UsfmUpdateBlockElement> _elements = new List<UsfmUpdateBlockElement>();
        private readonly List<UsfmUpdateBlockRow> _rows;

        public UsfmUpdateBlockBuilder(
            IEnumerable<ScriptureRef> refs = null,
            IEnumerable<UsfmUpdateBlockRow> rows = null
        )
        {
            _refs = refs?.ToList() ?? new List<ScriptureRef>();
            _rows = rows?.ToList() ?? new List<UsfmUpdateBlockRow>();
        }

        public IReadOnlyList<UsfmUpdateBlockElement> Elements => _elements;

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
            foreach (UsfmUpdateBlockElement element in Enumerable.Reverse(_elements))
            {
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

        public UsfmUpdateBlock Build()
        {
            return new UsfmUpdateBlock(_refs, _elements, _rows);
        }
    }
}
