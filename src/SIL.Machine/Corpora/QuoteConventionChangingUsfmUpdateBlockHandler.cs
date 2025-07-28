using System.Collections.Generic;
using SIL.Machine.Corpora.PunctuationAnalysis;

namespace SIL.Machine.Corpora
{
    public class QuoteConventionChangingUsfmUpdateBlockHandler : IUsfmUpdateBlockHandler
    {
        private readonly QuoteConvention _sourceQuoteConvention;
        private readonly QuoteConvention _targetQuoteConvention;
        private readonly QuotationMarkUpdateSettings _settings;
        protected QuotationMarkFinder _quotationMarkFinder;
        protected TextSegment.Builder _nextScriptureTextSegmentBuilder;
        protected IQuotationMarkResolver _verseTextQuotationMarkResolver;
        private readonly IQuotationMarkResolver _embedQuotationMarkResolver;
        private readonly IQuotationMarkResolver _simpleQuotationMarkResolver;
        protected QuotationMarkUpdateStrategy _currentStrategy;
        protected int _currentChapterNumber;
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

            _quotationMarkFinder = new QuotationMarkFinder(
                new QuoteConventionSet(new List<QuoteConvention> { _sourceQuoteConvention })
            );

            _nextScriptureTextSegmentBuilder = new TextSegment.Builder();

            IQuotationMarkResolutionSettings resolutionSettings = new QuotationMarkUpdateResolutionSettings(
                sourceQuoteConvention,
                targetQuoteConvention
            );

            // Each embed represents a separate context for quotation marks
            // (i.e. you can't open a quote in one context and close it in another)
            // so we need to keep track of the verse and embed contexts separately.
            _verseTextQuotationMarkResolver = new DepthBasedQuotationMarkResolver(resolutionSettings);
            _embedQuotationMarkResolver = new DepthBasedQuotationMarkResolver(resolutionSettings);
            _simpleQuotationMarkResolver = new FallbackQuotationMarkResolver(resolutionSettings);

            _currentStrategy = QuotationMarkUpdateStrategy.ApplyFull;
            _currentChapterNumber = 0;
            _currentVerseNumber = 0;
        }

        public UsfmUpdateBlock ProcessBlock(UsfmUpdateBlock block)
        {
            CheckForChapterChange(block);
            CheckForVerseChange(block);
            if (_currentStrategy == QuotationMarkUpdateStrategy.Skip)
                return block;
            if (_currentStrategy == QuotationMarkUpdateStrategy.ApplyFallback)
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
                    ProcessScriptureElement(element, _verseTextQuotationMarkResolver);
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
                _quotationMarkFinder.FindAllPotentialQuotationMarksInTextSegments(textSegments);
            foreach (
                QuotationMarkMetadata resolvedQuotationMark in quotationMarkResolver.ResolveQuotationMarks(
                    quotationMarkMatches
                )
            )
            {
                resolvedQuotationMark.UpdateQuotationMark(_targetQuoteConvention);
            }
        }

        protected List<TextSegment> CreateTextSegments(UsfmUpdateBlockElement element)
        {
            var textSegments = new List<TextSegment>();
            foreach (UsfmToken token in element.GetTokens())
            {
                switch (token.Type)
                {
                    case UsfmTokenType.Verse:
                        _nextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Verse);
                        break;
                    case UsfmTokenType.Paragraph:
                        _nextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Paragraph);
                        break;
                    case UsfmTokenType.Character:
                        _nextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Character);
                        break;
                    case UsfmTokenType.Note:
                        _nextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Embed);
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

        protected TextSegment CreateTextSegment(UsfmToken token)
        {
            TextSegment textSegmentToReturn = null;
            _nextScriptureTextSegmentBuilder.SetUsfmToken(token);
            if (token.Text != null)
            {
                _nextScriptureTextSegmentBuilder.SetText(token.Text);
                textSegmentToReturn = _nextScriptureTextSegmentBuilder.Build();
            }
            _nextScriptureTextSegmentBuilder = new TextSegment.Builder();
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
                if (scriptureRef.ChapterNum != _currentChapterNumber)
                {
                    _currentChapterNumber = scriptureRef.ChapterNum;
                    StartNewChapter(_currentChapterNumber);
                }
            }
        }

        protected void StartNewChapter(int newChapterNum)
        {
            _currentStrategy = _settings.GetActionForChapter(newChapterNum);
            _verseTextQuotationMarkResolver.Reset();
            _nextScriptureTextSegmentBuilder = new TextSegment.Builder();
            _nextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Chapter);
        }

        private void CheckForVerseChange(UsfmUpdateBlock block)
        {
            foreach (ScriptureRef scriptureRef in block.Refs)
            {
                if (scriptureRef.ChapterNum == _currentChapterNumber && scriptureRef.VerseNum != _currentVerseNumber)
                {
                    _currentVerseNumber = scriptureRef.VerseNum;
                    StartNewVerse();
                }
            }
        }

        private void StartNewVerse()
        {
            _nextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Verse);
        }
    }
}
