using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    /***
     * This is a USFM parser handler that can be used to replace the existing text in a USFM file with the specified
     * text.
     */
    public class UsfmTextUpdater : ScriptureRefUsfmParserHandlerBase
    {
        private readonly IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> _rows;
        private readonly List<UsfmToken> _tokens;
        private readonly string _idText;
        private readonly bool _stripAllText;
        private readonly bool _strictComparison;
        private readonly Stack<bool> _replace;
        private int _rowIndex;
        private int _tokenIndex;

        public UsfmTextUpdater(
            IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows = null,
            string idText = null,
            bool stripAllText = false,
            bool strictComparison = true
        )
        {
            _rows = rows ?? Array.Empty<(IReadOnlyList<ScriptureRef>, string)>();
            _tokens = new List<UsfmToken>();
            _idText = idText;
            _stripAllText = stripAllText;
            _strictComparison = strictComparison;
            _replace = new Stack<bool>();
        }

        public IReadOnlyList<UsfmToken> Tokens => _tokens;

        private bool ReplaceText => _stripAllText || (_replace.Count > 0 && _replace.Peek());

        public override void StartBook(UsfmParserState state, string marker, string code)
        {
            CollectTokens(state);
            if (_idText != null)
                _tokens.Add(new UsfmToken(_idText + " "));
            _replace.Push(_idText != null);

            base.StartBook(state, marker, code);
        }

        public override void EndBook(UsfmParserState state, string marker)
        {
            _replace.Pop();

            base.EndBook(state, marker);
        }

        public override void StartPara(
            UsfmParserState state,
            string marker,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            CollectTokens(state);

            base.StartPara(state, marker, unknown, attributes);
        }

        public override void StartRow(UsfmParserState state, string marker)
        {
            CollectTokens(state);

            base.StartRow(state, marker);
        }

        public override void StartCell(UsfmParserState state, string marker, string align, int colspan)
        {
            CollectTokens(state);

            base.StartCell(state, marker, align, colspan);
        }

        public override void EndCell(UsfmParserState state, string marker)
        {
            CollectTokens(state);

            base.EndCell(state, marker);
        }

        public override void StartSidebar(UsfmParserState state, string marker, string category)
        {
            CollectTokens(state);

            base.StartSidebar(state, marker, category);
        }

        public override void EndSidebar(UsfmParserState state, string marker, bool closed)
        {
            if (closed)
                CollectTokens(state);

            base.EndSidebar(state, marker, closed);
        }

        public override void Chapter(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            CollectTokens(state);

            base.Chapter(state, number, marker, altNumber, pubNumber);
        }

        public override void Milestone(
            UsfmParserState state,
            string marker,
            bool startMilestone,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            CollectTokens(state);

            base.Milestone(state, marker, startMilestone, attributes);
        }

        public override void Verse(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            CollectTokens(state);

            base.Verse(state, number, marker, altNumber, pubNumber);
        }

        public override void StartChar(
            UsfmParserState state,
            string markerWithoutPlus,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            // strip out char-style markers in verses that are being replaced
            if (ReplaceText)
                SkipTokens(state);
            else
                CollectTokens(state);

            base.StartChar(state, markerWithoutPlus, unknown, attributes);
        }

        public override void EndChar(
            UsfmParserState state,
            string marker,
            IReadOnlyList<UsfmAttribute> attributes,
            bool closed
        )
        {
            // strip out char-style markers in verses that are being replaced
            if (closed && ReplaceText)
                SkipTokens(state);

            base.EndChar(state, marker, attributes, closed);
        }

        public override void StartNote(UsfmParserState state, string marker, string caller, string category)
        {
            // strip out notes in verses that are being replaced
            if (ReplaceText)
                SkipTokens(state);
            else
                CollectTokens(state);

            base.StartNote(state, marker, caller, category);
        }

        public override void EndNote(UsfmParserState state, string marker, bool closed)
        {
            // strip out notes in verses that are being replaced
            if (closed && ReplaceText)
                SkipTokens(state);

            base.EndNote(state, marker, closed);
        }

        public override void Ref(UsfmParserState state, string marker, string display, string target)
        {
            // strip out ref in verses that are being replaced
            if (ReplaceText)
                SkipTokens(state);
            else
                CollectTokens(state);

            base.Ref(state, marker, display, target);
        }

        public override void Text(UsfmParserState state, string text)
        {
            // strip out text in verses that are being replaced
            if (ReplaceText)
                SkipTokens(state);
            else
                CollectTokens(state);

            base.Text(state, text);
        }

        public override void OptBreak(UsfmParserState state)
        {
            // strip out optbreaks in verses that are being replaced
            if (ReplaceText)
                SkipTokens(state);
            else
                CollectTokens(state);

            base.OptBreak(state);
        }

        public override void Unmatched(UsfmParserState state, string marker)
        {
            // strip out unmatched end markers in verses that are being replaced
            if (ReplaceText)
                SkipTokens(state);
            else
                CollectTokens(state);

            base.Unmatched(state, marker);
        }

        protected override void StartVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs)
        {
            IReadOnlyList<string> rowTexts = AdvanceRows(scriptureRefs);
            _tokens.AddRange(rowTexts.Select(t => new UsfmToken(t + " ")));
            _replace.Push(rowTexts.Count > 0);
        }

        protected override void EndVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs)
        {
            _replace.Pop();
        }

        protected override void StartNonVerseText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            IReadOnlyList<string> rowTexts = AdvanceRows(new[] { scriptureRef });
            _tokens.AddRange(rowTexts.Select(t => new UsfmToken(t + " ")));
            _replace.Push(rowTexts.Count > 0);
        }

        protected override void EndNonVerseText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            _replace.Pop();
        }

        protected override void StartNoteText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            IReadOnlyList<string> rowTexts = AdvanceRows(new[] { scriptureRef });
            if (rowTexts.Count > 0)
            {
                _tokens.Add(state.Token);
                _tokens.Add(new UsfmToken(UsfmTokenType.Character, "ft", null, "ft*"));
                for (int i = 0; i < rowTexts.Count; i++)
                {
                    string text = rowTexts[i];
                    if (i < rowTexts.Count - 1)
                        text += " ";
                    _tokens.Add(new UsfmToken(text));
                }
                _tokens.Add(new UsfmToken(UsfmTokenType.End, state.Token.EndMarker, null, null));
                _replace.Push(true);
            }
            else
            {
                _replace.Push(_replace.Peek());
            }
        }

        protected override void EndNoteText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            _replace.Pop();
        }

        public string GetUsfm(string stylesheetFileName = "usfm.sty")
        {
            return GetUsfm(new UsfmStylesheet(stylesheetFileName));
        }

        public string GetUsfm(UsfmStylesheet stylesheet)
        {
            var tokenizer = new UsfmTokenizer(stylesheet);
            return tokenizer.Detokenize(_tokens);
        }

        private IReadOnlyList<string> AdvanceRows(IReadOnlyList<ScriptureRef> segScrRefs)
        {
            var rowTexts = new List<string>();
            int i = 0;
            while (_rowIndex < _rows.Count && i < segScrRefs.Count)
            {
                (IReadOnlyList<ScriptureRef> rowScrRefs, string text) = _rows[_rowIndex];
                bool stop = false;
                foreach (ScriptureRef rowScrRef in rowScrRefs)
                {
                    bool found = false;
                    for (; i < segScrRefs.Count; i++)
                    {
                        int compare = rowScrRef.CompareTo(segScrRefs[i], compareSegments: false, _strictComparison);
                        if (compare == 0)
                        {
                            rowTexts.Add(text);
                            i++;
                            found = true;
                            break;
                        }
                        else if (compare > 0)
                        {
                            stop = true;
                            break;
                        }
                    }
                    if (stop || found)
                        break;
                }

                if (stop)
                    break;
                else
                    _rowIndex++;
            }
            return rowTexts;
        }

        private void CollectTokens(UsfmParserState state)
        {
            while (_tokenIndex <= state.Index + state.SpecialTokenCount)
            {
                _tokens.Add(state.Tokens[_tokenIndex]);
                _tokenIndex++;
            }
        }

        private void SkipTokens(UsfmParserState state)
        {
            _tokenIndex = state.Index + 1 + state.SpecialTokenCount;
        }
    }
}
