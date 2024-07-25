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
        private readonly List<UsfmToken> _newTokens;
        private readonly string _idText;
        private readonly bool _stripAllText;
        private readonly bool _preferExistingText;
        private readonly Stack<bool> _replace;
        private int _rowIndex;
        private int _tokenIndex;

        public UsfmTextUpdater(
            IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows = null,
            string idText = null,
            bool stripAllText = false,
            bool preferExistingText = false
        )
        {
            _rows = rows ?? Array.Empty<(IReadOnlyList<ScriptureRef>, string)>();
            _tokens = new List<UsfmToken>();
            _newTokens = new List<UsfmToken>();
            _idText = idText;
            _stripAllText = stripAllText;
            _replace = new Stack<bool>();
            _preferExistingText = preferExistingText;
        }

        public IReadOnlyList<UsfmToken> Tokens => _tokens;

        public override void EndUsfm(UsfmParserState state)
        {
            CollectTokens(state);
            base.EndUsfm(state);
        }

        public override void StartBook(UsfmParserState state, string marker, string code)
        {
            CollectTokens(state);
            var startBookTokens = new List<UsfmToken>();
            if (_idText != null)
                startBookTokens.Add(new UsfmToken(_idText + " "));
            PushNewTokens(startBookTokens);

            base.StartBook(state, marker, code);
        }

        public override void EndBook(UsfmParserState state, string marker)
        {
            PopNewTokens();

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
            if (ReplaceWithNewTokens(state))
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
            if (closed && ReplaceWithNewTokens(state))
                SkipTokens(state);

            base.EndChar(state, marker, attributes, closed);
        }

        public override void StartNote(UsfmParserState state, string marker, string caller, string category)
        {
            // strip out notes in verses that are being replaced
            if (ReplaceWithNewTokens(state))
                SkipTokens(state);
            else
                CollectTokens(state);

            base.StartNote(state, marker, caller, category);
        }

        public override void EndNote(UsfmParserState state, string marker, bool closed)
        {
            // strip out notes in verses that are being replaced
            if (closed && ReplaceWithNewTokens(state))
                SkipTokens(state);

            base.EndNote(state, marker, closed);
        }

        public override void Ref(UsfmParserState state, string marker, string display, string target)
        {
            // strip out ref in verses that are being replaced
            if (ReplaceWithNewTokens(state))
                SkipTokens(state);
            else
                CollectTokens(state);

            base.Ref(state, marker, display, target);
        }

        public override void Text(UsfmParserState state, string text)
        {
            // strip out text in verses that are being replaced
            if (ReplaceWithNewTokens(state))
                SkipTokens(state);
            else
                CollectTokens(state);

            base.Text(state, text);
        }

        public override void OptBreak(UsfmParserState state)
        {
            // strip out optbreaks in verses that are being replaced
            if (ReplaceWithNewTokens(state))
                SkipTokens(state);
            else
                CollectTokens(state);

            base.OptBreak(state);
        }

        public override void Unmatched(UsfmParserState state, string marker)
        {
            // strip out unmatched end markers in verses that are being replaced
            if (ReplaceWithNewTokens(state))
                SkipTokens(state);
            else
                CollectTokens(state);

            base.Unmatched(state, marker);
        }

        protected override void StartVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs)
        {
            IReadOnlyList<string> rowTexts = AdvanceRows(scriptureRefs);
            PushNewTokens(rowTexts.Select(t => new UsfmToken(t + " ")));
        }

        protected override void EndVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs)
        {
            PopNewTokens();
        }

        protected override void StartNonVerseText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            IReadOnlyList<string> rowTexts = AdvanceRows(new[] { scriptureRef });
            PushNewTokens(rowTexts.Select(t => new UsfmToken(t + " ")));
        }

        protected override void EndNonVerseText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            PopNewTokens();
        }

        protected override void StartNoteText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            IReadOnlyList<string> rowTexts = AdvanceRows(new[] { scriptureRef });
            var newTokens = new List<UsfmToken>();
            if (rowTexts.Count > 0)
            {
                newTokens.Add(state.Token);
                newTokens.Add(new UsfmToken(UsfmTokenType.Character, "ft", null, "ft*"));
                for (int i = 0; i < rowTexts.Count; i++)
                {
                    string text = rowTexts[i];
                    if (i < rowTexts.Count - 1)
                        text += " ";
                    newTokens.Add(new UsfmToken(text));
                }
                newTokens.Add(new UsfmToken(UsfmTokenType.End, state.Token.EndMarker, null, null));
                PushNewTokens(newTokens);
            }
            else
            {
                PushTokensAsPrevious();
            }
        }

        protected override void EndNoteText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            PopNewTokens();
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
            int sourceIndex = 0;
            // search the sorted rows with updated text, starting from where we left off last.
            while (_rowIndex < _rows.Count && sourceIndex < segScrRefs.Count)
            {
                // get the set of references for the current row
                int compare = 0;
                (IReadOnlyList<ScriptureRef> rowScrRefs, string text) = _rows[_rowIndex];
                foreach (ScriptureRef rowScrRef in rowScrRefs)
                {
                    while (sourceIndex < segScrRefs.Count)
                    {
                        compare = rowScrRef.CompareTo(segScrRefs[sourceIndex], compareSegments: false);
                        if (compare > 0)
                            // row is ahead of source, increment source
                            sourceIndex++;
                        else
                            break;
                    }
                    if (compare == 0)
                    {
                        // source and row match
                        // grab the text - both source and row will be incremented in due time...
                        rowTexts.Add(text);
                        break;
                    }
                }
                if (compare <= 0)
                {
                    // source is ahead row, increment row
                    _rowIndex++;
                }
            }
            return rowTexts;
        }

        private void CollectTokens(UsfmParserState state)
        {
            _tokens.AddRange(_newTokens);
            _newTokens.Clear();
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

        private bool ReplaceWithNewTokens(UsfmParserState state)
        {
            bool newText = _replace.Count > 0 && _replace.Peek();
            int tokenEnd = state.Index + state.SpecialTokenCount;
            bool existingText = false;
            for (int index = _tokenIndex; index <= tokenEnd; index++)
            {
                if (state.Tokens[index].Type == UsfmTokenType.Text && state.Tokens[index].Text.Length > 0)
                {
                    existingText = true;
                    break;
                }
            }
            bool useNewTokens = _stripAllText || (newText && !existingText) || (newText && !_preferExistingText);

            if (useNewTokens)
                _tokens.AddRange(_newTokens);

            _newTokens.Clear();
            return useNewTokens;
        }

        private void PushNewTokens(IEnumerable<UsfmToken> tokens)
        {
            _replace.Push(tokens.Any());
            _newTokens.AddRange(tokens);
        }

        private void PushTokensAsPrevious()
        {
            _replace.Push(_replace.Peek());
        }

        private void PopNewTokens()
        {
            // if (_replace.Any())
            _replace.Pop();
        }
    }
}
