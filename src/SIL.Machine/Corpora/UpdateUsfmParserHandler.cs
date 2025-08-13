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

    public class UpdateUsfmRow
    {
        public IReadOnlyList<ScriptureRef> Refs { get; }
        public string Text { get; }
        public IReadOnlyDictionary<string, object> Metadata { get; }

        public UpdateUsfmRow(
            IReadOnlyList<ScriptureRef> refs,
            string text,
            IReadOnlyDictionary<string, object> metadata = null
        )
        {
            Refs = refs;
            Text = text;
            Metadata = metadata ?? new Dictionary<string, object>();
        }
    }

    /***
     * This is a USFM parser handler that can be used to replace the existing text in a USFM file with the specified
     * text.
     */
    public class UpdateUsfmParserHandler : ScriptureRefUsfmParserHandlerBase
    {
        private readonly IReadOnlyList<UpdateUsfmRow> _rows;
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
        private readonly Stack<IUsfmUpdateBlockHandler> _updateBlockHandlers;
        private readonly List<string> _remarks;
        private readonly Stack<bool> _replace;
        private int _rowIndex;
        private int _tokenIndex;

        public UpdateUsfmParserHandler(
            IReadOnlyList<UpdateUsfmRow> rows = null,
            string idText = null,
            UpdateUsfmTextBehavior textBehavior = UpdateUsfmTextBehavior.PreferExisting,
            UpdateUsfmMarkerBehavior paragraphBehavior = UpdateUsfmMarkerBehavior.Preserve,
            UpdateUsfmMarkerBehavior embedBehavior = UpdateUsfmMarkerBehavior.Preserve,
            UpdateUsfmMarkerBehavior styleBehavior = UpdateUsfmMarkerBehavior.Strip,
            IEnumerable<string> preserveParagraphStyles = null,
            IEnumerable<IUsfmUpdateBlockHandler> updateBlockHandlers = null,
            IEnumerable<string> remarks = null
        )
        {
            _rows = rows ?? Array.Empty<UpdateUsfmRow>();
            _tokens = new List<UsfmToken>();
            _updatedText = new List<UsfmToken>();
            _updateBlocks = new Stack<UsfmUpdateBlock>();
            _embedTokens = new List<UsfmToken>();
            _idText = idText;
            _replace = new Stack<bool>();
            _textBehavior = textBehavior;
            _paragraphBehavior = paragraphBehavior;
            _embedBehavior = embedBehavior;
            _styleBehavior = styleBehavior;
            _updateBlockHandlers =
                updateBlockHandlers == null
                    ? new Stack<IUsfmUpdateBlockHandler>()
                    : new Stack<IUsfmUpdateBlockHandler>(updateBlockHandlers);
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
            if (state.IsVerseText)
            {
                // Only strip paragraph markers in a verse
                if (_paragraphBehavior == UpdateUsfmMarkerBehavior.Preserve)
                {
                    CollectUpdatableTokens(state);
                }
                else
                {
                    SkipUpdatableTokens(state);
                }
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

            CollectReadonlyTokens(state);
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

            // Ensure that a paragraph that contains a verse is not marked for removal
            if (_updateBlocks.Count > 0)
            {
                UsfmUpdateBlockElement lastParagraph = _updateBlocks.Peek().GetLastParagraph();
                if (lastParagraph != null)
                {
                    lastParagraph.MarkedForRemoval = false;
                }
            }

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
            CollectUpdatableTokens(state);
            StartUpdateBlock(scriptureRefs);
        }

        protected override void EndVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs)
        {
            EndUpdateBlock(state, scriptureRefs);
        }

        protected override void StartNonVerseText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            StartUpdateBlock(new[] { scriptureRef });
        }

        protected override void EndNonVerseText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            EndUpdateBlock(state, new[] { scriptureRef });
        }

        protected override void EndEmbedText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            _updateBlocks
                .Peek()
                .AddEmbed(_embedTokens, markedForRemoval: _embedBehavior == UpdateUsfmMarkerBehavior.Strip);
            _embedTokens.Clear();
        }

        public string GetUsfm(string stylesheetFileName = "usfm.sty")
        {
            return GetUsfm(new UsfmStylesheet(stylesheetFileName));
        }

        public string GetUsfm(UsfmStylesheet stylesheet)
        {
            var tokenizer = new UsfmTokenizer(stylesheet);
            List<UsfmToken> tokens = new List<UsfmToken>(_tokens);
            if (_remarks.Count() > 0)
            {
                var remarkTokens = new List<UsfmToken>();
                foreach (string remark in _remarks)
                {
                    remarkTokens.Add(new UsfmToken(UsfmTokenType.Paragraph, "rem", null, null));
                    remarkTokens.Add(new UsfmToken(remark));
                }

                if (tokens.Count > 0 && tokens[0].Marker == "id")
                {
                    int index = 1;
                    if (tokens.Count > 1 && tokens[1].Type == UsfmTokenType.Text)
                    {
                        index = 2;
                    }
                    while (tokens[index].Marker == "rem")
                    {
                        index++;
                        if (tokens.Count > index && tokens[index].Type == UsfmTokenType.Text)
                            index++;
                    }
                    tokens.InsertRange(index, remarkTokens);
                }
            }
            return tokenizer.Detokenize(tokens);
        }

        private (IReadOnlyList<string> RowTexts, Dictionary<string, object> Metadata) AdvanceRows(
            IReadOnlyList<ScriptureRef> segScrRefs
        )
        {
            var rowTexts = new List<string>();
            Dictionary<string, object> rowMetadata = null;
            int sourceIndex = 0;
            // search the sorted rows with updated text, starting from where we left off last.
            while (_rowIndex < _rows.Count && sourceIndex < segScrRefs.Count)
            {
                // get the set of references for the current row
                int compare = 0;
                UpdateUsfmRow row = _rows[_rowIndex];
                (IReadOnlyList<ScriptureRef> rowScrRefs, string text, IReadOnlyDictionary<string, object> metadata) = (
                    row.Refs,
                    row.Text,
                    row.Metadata
                );
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
                        rowMetadata = metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        break;
                    }
                }
                if (compare <= 0)
                {
                    // source is ahead row, increment row
                    _rowIndex++;
                }
            }
            return (rowTexts, rowMetadata);
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
                    (CurrentTextType != ScriptureTextType.None || state.ParaTag?.Marker == "id")
                    && _updateBlocks.Count > 0
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
                if (CurrentTextType != ScriptureTextType.None || (state.ParaTag?.Marker == "id"))
                {
                    if (_updateBlocks.Count > 0)
                    {
                        _updateBlocks.Peek().AddToken(token, markedForRemoval: true);
                    }
                }
                _tokenIndex++;
            }
            _tokenIndex = state.Index + 1 + state.SpecialTokenCount;
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
            (IReadOnlyList<string> rowTexts, Dictionary<string, object> metadata) = AdvanceRows(scriptureRefs);
            _updateBlocks.Push(
                new UsfmUpdateBlock(scriptureRefs, metadata: metadata ?? new Dictionary<string, object>())
            );
            PushUpdatedText(rowTexts.Select(t => new UsfmToken(t + " ")));
        }

        private void EndUpdateBlock(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs)
        {
            UseUpdatedText();
            PopNewTokens();
            UsfmUpdateBlock updateBlock = _updateBlocks.Pop();
            updateBlock.UpdateRefs(scriptureRefs);

            // Strip off any non-verse paragraphs that are at the end of the update block
            var paraElems = new List<UsfmUpdateBlockElement>();
            while (updateBlock.Elements.Count > 0 && IsNonverseParagraph(state, updateBlock.Elements.Last()))
            {
                paraElems.Add(updateBlock.Pop());
            }

            foreach (IUsfmUpdateBlockHandler handler in _updateBlockHandlers)
            {
                updateBlock = handler.ProcessBlock(updateBlock);
            }
            List<UsfmToken> tokens = updateBlock.GetTokens();
            foreach (UsfmUpdateBlockElement elem in Enumerable.Reverse(paraElems))
            {
                tokens.AddRange(elem.GetTokens());
            }
            if (
                _updateBlocks.Count > 0
                && _updateBlocks.Peek().Elements.Last().Type == UsfmUpdateBlockElementType.Paragraph
            )
            {
                _updateBlocks.Peek().ExtendLastElement(tokens);
            }
            else
            {
                _tokens.AddRange(tokens);
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

        private bool IsNonverseParagraph(UsfmParserState state, UsfmUpdateBlockElement element)
        {
            if (element.Type != UsfmUpdateBlockElementType.Paragraph)
                return false;
            UsfmToken paraToken = element.Tokens[0];
            if (paraToken.Marker is null)
                return false;
            UsfmTag paraTag = state.Stylesheet.GetTag(paraToken.Marker);
            return paraTag.TextType != UsfmTextType.VerseText && paraTag.TextType != UsfmTextType.NotSpecified;
        }
    }
}
