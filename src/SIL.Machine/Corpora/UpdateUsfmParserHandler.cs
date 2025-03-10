using System;
using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

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
        private readonly List<UsfmToken> _newTokens;
        private readonly List<UsfmToken> _newEmbedTokens;
        private readonly string _idText;
        private readonly UpdateUsfmTextBehavior _textBehavior;
        private readonly UpdateUsfmMarkerBehavior _paragraphBehavior;
        private readonly UpdateUsfmMarkerBehavior _embedBehavior;
        private readonly UpdateUsfmMarkerBehavior _styleBehavior;
        private readonly IReadOnlySet<string> _preserveParagraphStyles;
        private readonly Stack<bool> _replace;
        private int _rowIndex;
        private int _tokenIndex;
        private bool _embedUpdated;
        private bool _inPreservedParagraph;
        private List<string> _embedRowTexts;

        public UpdateUsfmParserHandler(
            IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows = null,
            string idText = null,
            UpdateUsfmTextBehavior textBehavior = UpdateUsfmTextBehavior.PreferExisting,
            UpdateUsfmMarkerBehavior paragraphBehavior = UpdateUsfmMarkerBehavior.Preserve,
            UpdateUsfmMarkerBehavior embedBehavior = UpdateUsfmMarkerBehavior.Preserve,
            UpdateUsfmMarkerBehavior styleBehavior = UpdateUsfmMarkerBehavior.Strip,
            IReadOnlyCollection<string> preserveParagraphStyles = null
        )
        {
            _rows = rows ?? Array.Empty<(IReadOnlyList<ScriptureRef>, string)>();
            _tokens = new List<UsfmToken>();
            _newTokens = new List<UsfmToken>();
            _newEmbedTokens = new List<UsfmToken>();
            _idText = idText;
            _replace = new Stack<bool>();
            _textBehavior = textBehavior;
            _paragraphBehavior = paragraphBehavior;
            _embedBehavior = embedBehavior;
            _styleBehavior = styleBehavior;
            if (preserveParagraphStyles == null)
                _preserveParagraphStyles = (IReadOnlySet<string>)new HashSet<string> { "r", "rem" };
            else
                _preserveParagraphStyles = (IReadOnlySet<string>)new HashSet<string>(preserveParagraphStyles);
            _embedUpdated = false;
            _embedRowTexts = new List<string>();
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
            if (marker != null && _preserveParagraphStyles.Contains(marker))
            {
                _inPreservedParagraph = true;
            }
            if (
                state.IsVerseText
                && (HasNewText() || _textBehavior == UpdateUsfmTextBehavior.StripExisting)
                && _paragraphBehavior == UpdateUsfmMarkerBehavior.Strip
            )
            {
                SkipTokens(state);
            }
            else
            {
                CollectTokens(state);
            }

            base.StartPara(state, marker, unknown, attributes);
        }

        public override void EndPara(UsfmParserState state, string marker)
        {
            base.EndPara(state, marker);
            _inPreservedParagraph = false;
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
            if (ReplaceWithNewTokens(state, closed: closed))
                SkipTokens(state);
            else
                CollectTokens(state);

            base.EndChar(state, marker, attributes, closed);
        }

        protected override void StartEmbed(UsfmParserState state, ScriptureRef scriptureRef)
        {
            _embedRowTexts = AdvanceRows(new[] { scriptureRef }).ToList();
            _embedUpdated = _embedRowTexts.Count > 0;

            // strip out notes in verses that are being replaced
            if (ReplaceWithNewTokens(state))
                SkipTokens(state);
            else
                CollectTokens(state);
        }

        protected override void EndEmbed(
            UsfmParserState state,
            string marker,
            IReadOnlyList<UsfmAttribute> attributes,
            bool closed
        )
        {
            // strip out notes in verses that are being replaced
            if (ReplaceWithNewTokens(state, closed: closed))
                SkipTokens(state);
            else
                CollectTokens(state);

            _embedRowTexts.Clear();
            _embedUpdated = false;

            base.EndEmbed(state, marker, attributes, closed);
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
            base.Text(state, text);

            // strip out text in verses that are being replaced
            if (ReplaceWithNewTokens(state))
                SkipTokens(state);
            else
                CollectTokens(state);
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

        protected override void StartNoteText(UsfmParserState state)
        {
            PushNewEmbedTokens(_embedRowTexts.Select(t => new UsfmToken(t + " ")));
        }

        protected override void EndNoteText(UsfmParserState state, ScriptureRef scriptureRef)
        {
            _embedRowTexts.Clear();
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

        private bool ReplaceWithNewTokens(UsfmParserState state, bool closed = true)
        {
            string marker = state?.Token?.Marker;
            bool inEmbed = IsInEmbed(marker);

            bool inNestedEmbed = IsInNestedEmbed(marker);
            bool isStyleTag = marker != null && !IsEmbedPartStyle(marker);

            bool existingText = state
                .Tokens.Skip(_tokenIndex)
                .Take(state.Index + 1 + state.SpecialTokenCount - _tokenIndex)
                .Any(t => t.Type == UsfmTokenType.Text && t.Text.Length > 0);

            bool useNewTokens =
                (
                    (_textBehavior == UpdateUsfmTextBehavior.StripExisting && !IsInPreservedParagraph(marker))
                    || (HasNewText() && (!existingText || _textBehavior != UpdateUsfmTextBehavior.PreferExisting))
                )
                && (!inEmbed || (InNoteText && !inNestedEmbed && _embedBehavior == UpdateUsfmMarkerBehavior.Preserve));

            if (useNewTokens)
            {
                if (inEmbed)
                    AddNewEmbedTokens();
                else
                    AddNewTokens();
            }

            if (existingText && _textBehavior == UpdateUsfmTextBehavior.PreferExisting)
            {
                if (inEmbed)
                    ClearNewEmbedTokens();
                else
                    ClearNewTokens();
            }

            // figure out when to skip the existing text
            bool embedInNewVerseText =
                (_replace.Any(r => r) || _textBehavior == UpdateUsfmTextBehavior.StripExisting) && inEmbed;
            if (embedInNewVerseText || _embedUpdated)
            {
                if (_embedBehavior == UpdateUsfmMarkerBehavior.Strip)
                {
                    ClearNewEmbedTokens();
                    return true;
                }

                if (!InNoteText || inNestedEmbed)
                    return false;
            }

            bool skipTokens = useNewTokens && closed;

            if (useNewTokens && isStyleTag)
            {
                skipTokens = _styleBehavior == UpdateUsfmMarkerBehavior.Strip;
            }
            return skipTokens;
        }

        private bool HasNewText()
        {
            return _replace.Count > 0 && _replace.Peek();
        }

        private void PushNewTokens(IEnumerable<UsfmToken> tokens)
        {
            _replace.Push(tokens.Any());
            _newTokens.AddRange(tokens);
        }

        private void AddNewTokens()
        {
            if (_newTokens.Count > 0)
                _tokens.AddRange(_newTokens);
            _newTokens.Clear();
        }

        private void ClearNewTokens()
        {
            _newTokens.Clear();
        }

        private void PushNewEmbedTokens(IEnumerable<UsfmToken> tokens)
        {
            _replace.Push(tokens.Any());
            if (tokens.Any())
                _newEmbedTokens.AddRange(tokens);
        }

        private void AddNewEmbedTokens()
        {
            if (_newEmbedTokens.Count > 0)
                _tokens.AddRange(_newEmbedTokens);
            _newEmbedTokens.Clear();
        }

        private void ClearNewEmbedTokens()
        {
            _newEmbedTokens.Clear();
        }

        private void PopNewTokens()
        {
            _replace.Pop();
        }

        private bool IsInPreservedParagraph(string marker)
        {
            return _inPreservedParagraph || _preserveParagraphStyles.Contains(marker);
        }
    }
}
