using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public enum UpdateUsfmTextBehavior
    {
        PreferExisting,
        PreferNew,
        StripExisting
    }

    public enum UpdateUsfmMarkerBehavior
    {
        Preserve,
        Strip,
    }

    /***
     * This is a USFM parser handler that can be used to replace the existing text in a USFM file with the specified
     * text.
     */
    public class UpdateUsfmParserHandler : ScriptureRefUsfmParserHandlerBase
    {
        private readonly IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> _rows;
        private readonly List<UsfmToken> _tokens;
        private readonly List<UsfmToken> _updatedText;
        private readonly List<UsfmToken> _embedTokens;
        private readonly string _idText;
        private readonly UpdateUsfmTextBehavior _textBehavior;
        private readonly UpdateUsfmMarkerBehavior _paragraphBehavior;
        private readonly UpdateUsfmMarkerBehavior _embedBehavior;
        private readonly UpdateUsfmMarkerBehavior _styleBehavior;
        private readonly HashSet<string> _preserveParagraphStyles;
        private readonly Stack<UsfmUpdateBlock> _updateBlocks;
        private readonly Stack<UsfmUpdateBlockHandler> _updateBlockHandlers;
        private readonly List<string> _remarks;
        private readonly Stack<bool> _replace;
        private int _rowIndex;
        private int _tokenIndex;

        public UpdateUsfmParserHandler(
            IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows = null,
            string idText = null,
            UpdateUsfmTextBehavior textBehavior = UpdateUsfmTextBehavior.PreferExisting,
            UpdateUsfmMarkerBehavior paragraphBehavior = UpdateUsfmMarkerBehavior.Preserve,
            UpdateUsfmMarkerBehavior embedBehavior = UpdateUsfmMarkerBehavior.Preserve,
            UpdateUsfmMarkerBehavior styleBehavior = UpdateUsfmMarkerBehavior.Strip,
            IEnumerable<string> preserveParagraphStyles = null,
            IEnumerable<UsfmUpdateBlockHandler> updateBlockHandlers = null,
            IEnumerable<string> remarks = null
        )
        {
            _rows = rows ?? Array.Empty<(IReadOnlyList<ScriptureRef>, string)>();
            _tokens = new List<UsfmToken>();
            _updatedText = new List<UsfmToken>();
            _embedTokens = new List<UsfmToken>();
            _idText = idText;
            _replace = new Stack<bool>();
            _textBehavior = textBehavior;
            _paragraphBehavior = paragraphBehavior;
            _embedBehavior = embedBehavior;
            _styleBehavior = styleBehavior;
            _updateBlockHandlers =
                updateBlockHandlers == null
                    ? new Stack<UsfmUpdateBlockHandler>()
                    : new Stack<UsfmUpdateBlockHandler>(updateBlockHandlers);
            _preserveParagraphStyles =
                preserveParagraphStyles == null
                    ? new HashSet<string> { "r", "rem" }
                    : new HashSet<string>(preserveParagraphStyles);
            _remarks = remarks == null ? new List<string>() : remarks.ToList();
        }

        public IReadOnlyList<UsfmToken> Tokens => _tokens;

        public override void EndUsfm(UsfmParserState state)
        {
            CollectUpdatableTokens(state);
            base.EndUsfm(state);
        }

        public override void StartBook(UsfmParserState state, string marker, string code)
        {
            CollectReadonlyTokens(state);
            _updateBlocks.Push(new UsfmUpdateBlock());
            var startBookTokens = new List<UsfmToken>();
            if (_idText != null)
                startBookTokens.Add(new UsfmToken(_idText + " "));
            if (_remarks.Count() > 0)
            {
                foreach (string remark in _remarks)
                {
                    startBookTokens.Add(new UsfmToken(UsfmTokenType.Paragraph, "rem", null, null));
                    startBookTokens.Add(new UsfmToken(remark));
                }
            }
            PushUpdatedText(startBookTokens);

            base.StartBook(state, marker, code);
        }

        public override void EndBook(UsfmParserState state, string marker)
        {
            UseUpdatedText();
            PopNewTokens();
            UsfmUpdateBlock updateBlock = _updateBlocks.Pop();
            _tokens.AddRange(updateBlock.GetTokens());
            base.EndBook(state, marker);
        }

        public override void StartPara(
            UsfmParserState state,
            string marker,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            if (
                state.IsVerseText
                && (HasNewText() || _textBehavior == UpdateUsfmTextBehavior.StripExisting)
                && _paragraphBehavior == UpdateUsfmMarkerBehavior.Strip
            )
            {
                SkipUpdatableTokens(state);
            }
            else
            {
                CollectUpdatableTokens(state);
            }

            base.StartPara(state, marker, unknown, attributes);
        }

        public override void StartRow(UsfmParserState state, string marker)
        {
            CollectUpdatableTokens(state);

            base.StartRow(state, marker);
        }

        public override void StartCell(UsfmParserState state, string marker, string align, int colspan)
        {
            CollectUpdatableTokens(state);

            base.StartCell(state, marker, align, colspan);
        }

        public override void StartSidebar(UsfmParserState state, string marker, string category)
        {
            CollectUpdatableTokens(state);

            base.StartSidebar(state, marker, category);
        }

        public override void EndSidebar(UsfmParserState state, string marker, bool closed)
        {
            if (closed)
                CollectUpdatableTokens(state);

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
            UseUpdatedText();

            base.Chapter(state, number, marker, altNumber, pubNumber);
        }

        public override void Milestone(
            UsfmParserState state,
            string marker,
            bool startMilestone,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            CollectUpdatableTokens(state);

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
            UseUpdatedText();

            base.Verse(state, number, marker, altNumber, pubNumber);

            CollectReadonlyTokens(state);
        }

        public override void StartNote(UsfmParserState state, string marker, string caller, string category)
        {
            base.StartNote(state, marker, caller, category);

            CollectUpdatableTokens(state);
        }

        public override void EndNote(UsfmParserState state, string marker, bool closed)
        {
            if (closed)
                CollectUpdatableTokens(state);
            base.EndNote(state, marker, closed);
        }

        public override void StartChar(
            UsfmParserState state,
            string markerWithoutPlus,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            base.StartChar(state, markerWithoutPlus, unknown, attributes);

            if (CurrentTextType == ScriptureTextType.Embed)
            {
                CollectUpdatableTokens(state);
            }
            else
            {
                ReplaceWithNewTokens(state);
                if (_styleBehavior == UpdateUsfmMarkerBehavior.Strip)
                {
                    SkipUpdatableTokens(state);
                }
                else
                {
                    CollectUpdatableTokens(state);
                }
            }
        }

        public override void EndChar(
            UsfmParserState state,
            string marker,
            IReadOnlyList<UsfmAttribute> attributes,
            bool closed
        )
        {
            // strip out char-style markers in verses that are being replaced
            if (CurrentTextType == ScriptureTextType.Embed)
            {
                CollectUpdatableTokens(state);
            }
            else
            {
                ReplaceWithNewTokens(state);
                if (_styleBehavior == UpdateUsfmMarkerBehavior.Strip)
                {
                    SkipUpdatableTokens(state);
                }
                else
                {
                    CollectUpdatableTokens(state);
                }
            }

            base.EndChar(state, marker, attributes, closed);
        }

        public override void Ref(UsfmParserState state, string marker, string display, string target)
        {
            base.Ref(state, marker, display, target);

            if (ReplaceWithNewTokens(state))
                SkipUpdatableTokens(state);
            else
                CollectUpdatableTokens(state);
        }

        public override void Text(UsfmParserState state, string text)
        {
            base.Text(state, text);

            // strip out text in verses that are being replaced
            if (ReplaceWithNewTokens(state))
                SkipUpdatableTokens(state);
            else
                CollectUpdatableTokens(state);
        }

        public override void OptBreak(UsfmParserState state)
        {
            base.OptBreak(state);
            if (ReplaceWithNewTokens(state))
                SkipUpdatableTokens(state);
            else
                CollectUpdatableTokens(state);
        }

        public override void Unmatched(UsfmParserState state, string marker)
        {
            base.Unmatched(state, marker);

            if (ReplaceWithNewTokens(state))
                SkipUpdatableTokens(state);
            else
                CollectUpdatableTokens(state);
        }

        protected override void StartVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs)
        {
            StartUpdateBlock(scriptureRefs);
        }

        protected override void EndVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs)
        {
            EndUpdateBlock(scriptureRefs);
        }

        protected override void StartNonVerseText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            StartUpdateBlock(new[] { scriptureRef });
        }

        protected override void EndNonVerseText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            EndUpdateBlock(new[] { scriptureRef });
        }

        protected override void EndEmbedText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            _updateBlocks
                .Peek()
                .AddEmbed(_embedTokens, markedForRemoval: _embedBehavior == UpdateUsfmMarkerBehavior.Strip);
            base.EndEmbedText(state, scriptureRef);
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

        private void CollectUpdatableTokens(UsfmParserState state)
        {
            UseUpdatedText();
            while (_tokenIndex <= state.Index + state.SpecialTokenCount)
            {
                UsfmToken token = state.Tokens[_tokenIndex];
                if (CurrentTextType == ScriptureTextType.Embed)
                {
                    _embedTokens.Add(token);
                }
                else if (
                    CurrentTextType != ScriptureTextType.None
                    || (state.ParaTag != null && state.ParaTag.Marker == "id") && _updateBlocks.Count > 0
                )
                {
                    _updateBlocks.Peek().AddToken(token);
                }
                else
                {
                    _tokens.Add(token);
                }
                _tokenIndex++;
            }
        }

        private void CollectReadonlyTokens(UsfmParserState state)
        {
            while (_tokenIndex <= state.Index + state.SpecialTokenCount)
            {
                UsfmToken token = state.Tokens[_tokenIndex];
                if (_updateBlocks.Count > 0)
                {
                    _updateBlocks.Peek().AddToken(token);
                }
                else
                {
                    _tokens.Add(token);
                }
                _tokenIndex++;
            }
        }

        private void SkipUpdatableTokens(UsfmParserState state)
        {
            while (_tokenIndex <= state.Index + state.SpecialTokenCount)
            {
                UsfmToken token = state.Tokens[_tokenIndex];
                if (
                    CurrentTextType != ScriptureTextType.None
                    || (state.ParaTag != null && state.ParaTag.Marker == "id")
                )
                {
                    if (_updateBlocks.Count > 0)
                    {
                        _updateBlocks.Peek().AddToken(token);
                    }
                    _tokenIndex++;
                }
            }
            _tokenIndex = state.Index + state.SpecialTokenCount + 1;
        }

        private bool ReplaceWithNewTokens(UsfmParserState state)
        {
            if (CurrentTextType == ScriptureTextType.Embed)
                return false;

            bool existingText = state
                .Tokens.Skip(_tokenIndex)
                .Take(state.Index + 1 + state.SpecialTokenCount - _tokenIndex)
                .Any(t => t.Type == UsfmTokenType.Text && t.Text.Length > 0);

            bool useNewTokens = true;
            if (IsInPreservedParagraph(state))
            {
                useNewTokens = false;
            }
            else if (
                _textBehavior != UpdateUsfmTextBehavior.StripExisting
                && (!HasNewText() || (existingText && _textBehavior == UpdateUsfmTextBehavior.PreferExisting))
            )
            {
                useNewTokens = false;
            }

            if (useNewTokens)
                UseUpdatedText();

            bool clearNewTokens =
                existingText
                && (_textBehavior == UpdateUsfmTextBehavior.PreferExisting || IsInPreservedParagraph(state));
            if (clearNewTokens)
                ClearUpdatedText();

            return useNewTokens;
        }

        private bool HasNewText()
        {
            return _replace.Count > 0 && _replace.Peek();
        }

        private void StartUpdateBlock(IReadOnlyList<ScriptureRef> scriptureRefs)
        {
            _updateBlocks.Push(new UsfmUpdateBlock(scriptureRefs));
            IReadOnlyList<string> rowTexts = AdvanceRows(scriptureRefs);
            PushUpdatedText(rowTexts.Select(t => new UsfmToken(t + " ")));
        }

        private void EndUpdateBlock(IReadOnlyList<ScriptureRef> scriptureRefs)
        {
            UseUpdatedText();
            PopNewTokens();
            UsfmUpdateBlock updateBlock = _updateBlocks.Pop();
            updateBlock.UpdateRefs(scriptureRefs);
            foreach (UsfmUpdateBlockHandler handler in _updateBlockHandlers)
            {
                updateBlock = handler.ProcessBlock(updateBlock);
            }
            if (
                _updateBlocks.Count > 0
                && _updateBlocks.Peek().Elements.Last().Type == UsfmUpdateBlockElementType.Paragraph
            )
            {
                _updateBlocks.Peek().ExtendLastElement(updateBlock.GetTokens());
            }
            else
            {
                _tokens.AddRange(updateBlock.GetTokens());
            }
        }

        private void PushUpdatedText(IEnumerable<UsfmToken> tokens)
        {
            _replace.Push(tokens.Any());
            if (tokens.Any())
                _updatedText.AddRange(tokens);
        }

        private void UseUpdatedText()
        {
            if (_updatedText.Count > 0)
                _updateBlocks.Peek().AddText(_updatedText);
            _updatedText.Clear();
        }

        private void ClearUpdatedText()
        {
            _updatedText.Clear();
        }

        private void PopNewTokens()
        {
            _replace.Pop();
        }

        private bool IsInPreservedParagraph(UsfmParserState state)
        {
            return state.ParaTag != null && _preserveParagraphStyles.Contains(state.ParaTag.Marker);
        }
    }
}
