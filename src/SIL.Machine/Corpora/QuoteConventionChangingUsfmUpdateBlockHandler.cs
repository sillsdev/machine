using System.Collections.Generic;
using System.Linq;
using SIL.Machine.PunctuationAnalysis;

namespace SIL.Machine.Corpora
{
    public class QuoteConventionChangingUsfmUpdateBlockHandler : IUsfmUpdateBlockHandler
    {
        private readonly QuoteConvention _sourceQuoteConvention;
        private readonly QuoteConvention _targetQuoteConvention;
        private readonly QuotationMarkUpdateSettings _settings;
        protected QuotationMarkFinder QuotationMarkFinder { get; set; }
        protected TextSegment.Builder NextScriptureTextSegmentBuilder { get; set; }
        protected IQuotationMarkResolver VerseTextQuotationMarkResolver { get; set; }
        private readonly IQuotationMarkResolver _embedQuotationMarkResolver;
        private readonly IQuotationMarkResolver _simpleQuotationMarkResolver;
        protected QuotationMarkUpdateStrategy CurrentStrategy { get; set; }
        protected int CurrentChapterNumber { get; set; }
        private int _currentVerseNumber;

        public QuoteConventionChangingUsfmUpdateBlockHandler(
            QuoteConvention sourceQuoteConvention,
            QuoteConvention targetQuoteConvention,
            QuotationMarkUpdateSettings settings
        )
        {
            _sourceQuoteConvention = sourceQuoteConvention;
            _targetQuoteConvention = targetQuoteConvention;
            _settings = settings;

            QuotationMarkFinder = new QuotationMarkFinder(
                new QuoteConventionSet(new List<QuoteConvention> { _sourceQuoteConvention })
            );

            NextScriptureTextSegmentBuilder = new TextSegment.Builder();

            IQuotationMarkResolutionSettings resolutionSettings = new QuotationMarkUpdateResolutionSettings(
                sourceQuoteConvention
            );

            // Each embed represents a separate context for quotation marks
            // (i.e. you can't open a quote in one context and close it in another)
            // so we need to keep track of the verse and embed contexts separately.
            VerseTextQuotationMarkResolver = new DepthBasedQuotationMarkResolver(resolutionSettings);
            _embedQuotationMarkResolver = new DepthBasedQuotationMarkResolver(resolutionSettings);
            _simpleQuotationMarkResolver = new FallbackQuotationMarkResolver(resolutionSettings);

            CurrentStrategy = QuotationMarkUpdateStrategy.ApplyFull;
            CurrentChapterNumber = 0;
            _currentVerseNumber = 0;
        }

        public UsfmUpdateBlock ProcessBlock(UsfmUpdateBlock block)
        {
            CheckForChapterChange(block);
            CheckForVerseChange(block);
            if (CurrentStrategy == QuotationMarkUpdateStrategy.Skip)
                return block;
            if (CurrentStrategy == QuotationMarkUpdateStrategy.ApplyFallback)
            {
                return ApplyFallbackUpdating(block);
            }
            return ApplyStandardUpdating(block);
        }

        private UsfmUpdateBlock ApplyFallbackUpdating(UsfmUpdateBlock block)
        {
            foreach (UsfmUpdateBlockElement element in block.Elements)
                ProcessScriptureElement(element, _simpleQuotationMarkResolver);
            return block;
        }

        private UsfmUpdateBlock ApplyStandardUpdating(UsfmUpdateBlock block)
        {
            foreach (UsfmUpdateBlockElement element in block.Elements)
            {
                if (element.Type == UsfmUpdateBlockElementType.Embed)
                {
                    _embedQuotationMarkResolver.Reset();
                    ProcessScriptureElement(element, _embedQuotationMarkResolver);
                }
                else
                {
                    ProcessScriptureElement(element, VerseTextQuotationMarkResolver);
                }
            }
            return block;
        }

        protected void ProcessScriptureElement(
            UsfmUpdateBlockElement element,
            IQuotationMarkResolver quotationMarkResolver
        )
        {
            List<TextSegment> textSegments = CreateTextSegments(element);
            List<QuotationMarkStringMatch> quotationMarkMatches =
                QuotationMarkFinder.FindAllPotentialQuotationMarksInTextSegments(textSegments);
            List<QuotationMarkMetadata> resolvedQuotationMarkMatches = quotationMarkResolver
                .ResolveQuotationMarks(quotationMarkMatches)
                .ToList();
            UpdateQuotationMarks(resolvedQuotationMarkMatches);
        }

        protected List<TextSegment> CreateTextSegments(UsfmUpdateBlockElement element)
        {
            var textSegments = new List<TextSegment>();
            foreach (UsfmToken token in element.GetTokens())
            {
                switch (token.Type)
                {
                    case UsfmTokenType.Verse:
                        NextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Verse);
                        break;
                    case UsfmTokenType.Paragraph:
                        NextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Paragraph);
                        break;
                    case UsfmTokenType.Character:
                        NextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Character);
                        break;
                    case UsfmTokenType.Note:
                        NextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Embed);
                        break;
                    case UsfmTokenType.Text:
                        TextSegment textSegment = CreateTextSegment(token);
                        if (textSegment != null)
                            textSegments.Add(textSegment);
                        break;
                }
            }
            return SetPreviousAndNextForSegments(textSegments);
        }

        public void UpdateQuotationMarks(List<QuotationMarkMetadata> resolvedQuotationMarkMatches)
        {
            foreach (
                (
                    int quotationMarkIndex,
                    QuotationMarkMetadata resolvedQuotationMarkMatch
                ) in resolvedQuotationMarkMatches.Select((r, i) => (i, r))
            )
            {
                int previousLength = resolvedQuotationMarkMatch.Length;
                resolvedQuotationMarkMatch.UpdateQuotationMark(_targetQuoteConvention);
                int updatedLength = resolvedQuotationMarkMatch.Length;

                if (previousLength != updatedLength)
                {
                    ShiftQuotationMarkMetadataIndices(
                        resolvedQuotationMarkMatches.Skip(quotationMarkIndex + 1).ToList(),
                        updatedLength - previousLength
                    );
                }
            }
        }

        private void ShiftQuotationMarkMetadataIndices(
            List<QuotationMarkMetadata> quotationMarkMetadataList,
            int shiftAmount
        )
        {
            foreach (QuotationMarkMetadata quotationMarkMetadata in quotationMarkMetadataList)
            {
                quotationMarkMetadata.ShiftIndices(shiftAmount);
            }
        }

        protected TextSegment CreateTextSegment(UsfmToken token)
        {
            TextSegment textSegmentToReturn = null;
            NextScriptureTextSegmentBuilder.SetUsfmToken(token);
            if (token.Text != null)
            {
                NextScriptureTextSegmentBuilder.SetText(token.Text);
                textSegmentToReturn = NextScriptureTextSegmentBuilder.Build();
            }
            NextScriptureTextSegmentBuilder = new TextSegment.Builder();
            return textSegmentToReturn;
        }

        protected List<TextSegment> SetPreviousAndNextForSegments(List<TextSegment> textSegments)
        {
            for (int i = 0; i < textSegments.Count; i++)
            {
                if (i > 0)
                    textSegments[i].PreviousSegment = textSegments[i - 1];
                if (i < textSegments.Count - 1)
                    textSegments[i].NextSegment = textSegments[i + 1];
            }
            return textSegments;
        }

        protected void CheckForChapterChange(UsfmUpdateBlock block)
        {
            foreach (ScriptureRef scriptureRef in block.Refs)
            {
                if (scriptureRef.ChapterNum != CurrentChapterNumber)
                {
                    StartNewChapter(scriptureRef.ChapterNum);
                }
            }
        }

        protected void StartNewChapter(int newChapterNumber)
        {
            CurrentChapterNumber = newChapterNumber;
            CurrentStrategy = _settings.GetActionForChapter(newChapterNumber);
            VerseTextQuotationMarkResolver.Reset();
            NextScriptureTextSegmentBuilder = new TextSegment.Builder();
            NextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Chapter);
        }

        private void CheckForVerseChange(UsfmUpdateBlock block)
        {
            foreach (ScriptureRef scriptureRef in block.Refs)
            {
                if (scriptureRef.ChapterNum == CurrentChapterNumber && scriptureRef.VerseNum != _currentVerseNumber)
                {
                    StartNewVerse(scriptureRef.VerseNum);
                }
            }
        }

        private void StartNewVerse(int newVerseNumber)
        {
            _currentVerseNumber = newVerseNumber;
            NextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Verse);
        }
    }
}
