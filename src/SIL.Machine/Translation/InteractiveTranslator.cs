using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Annotations;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Translation
{
    public class InteractiveTranslator
    {
        private readonly IInteractiveTranslationEngine _engine;
        private readonly StringBuilder _prefix;
        private readonly ErrorCorrectionWordGraphProcessor _wordGraphProcessor;
        private readonly IRangeTokenizer<string, int, string> _targetTokenizer;

        internal InteractiveTranslator(
            string segment,
            ErrorCorrectionModel ecm,
            IInteractiveTranslationEngine engine,
            IRangeTokenizer<string, int, string> targetTokenizer,
            IDetokenizer<string, string> targetDetokenizer,
            WordGraph wordGraph
        )
        {
            Segment = segment;
            SegmentWordRanges = Segment.GetRanges(wordGraph.SourceTokens).ToArray();
            _engine = engine;
            _targetTokenizer = targetTokenizer;
            PrefixWordRanges = Array.Empty<Range<int>>();
            _prefix = new StringBuilder();
            IsLastWordComplete = true;
            _wordGraphProcessor = new ErrorCorrectionWordGraphProcessor(ecm, targetDetokenizer, wordGraph);
            Correct();
        }

        public string Segment { get; }
        public IReadOnlyList<Range<int>> SegmentWordRanges { get; }
        public string Prefix => _prefix.ToString();
        public IReadOnlyList<Range<int>> PrefixWordRanges { get; private set; }
        public bool IsLastWordComplete { get; private set; }

        public bool IsSegmentValid => SegmentWordRanges.Count <= TranslationConstants.MaxSegmentLength;

        public void SetPrefix(string prefix)
        {
            if (_prefix.ToString() != prefix)
            {
                _prefix.Clear();
                _prefix.Append(prefix);
                Correct();
            }
        }

        public void AppendToPrefix(string addition)
        {
            if (!string.IsNullOrEmpty(addition))
            {
                _prefix.Append(addition);
                Correct();
            }
        }

        public async Task ApproveAsync(bool alignedOnly, CancellationToken cancellationToken = default)
        {
            if (!IsSegmentValid || PrefixWordRanges.Count > TranslationConstants.MaxSegmentLength)
                return;

            IReadOnlyList<Range<int>> segmentWordRanges = SegmentWordRanges;
            if (alignedOnly)
            {
                TranslationResult bestResult = GetCurrentResults().FirstOrDefault();
                if (bestResult == null)
                    return;
                segmentWordRanges = GetAlignedSourceSegment(bestResult);
            }

            if (segmentWordRanges.Count > 0)
            {
                string sourceSegment = Segment.Substring(
                    segmentWordRanges.First().Start,
                    segmentWordRanges.Last().End - segmentWordRanges.First().Start
                );
                string targetSegment = _prefix
                    .ToString()
                    .Substring(
                        PrefixWordRanges.First().Start,
                        PrefixWordRanges.Last().End - PrefixWordRanges.First().Start
                    );
                await _engine
                    .TrainSegmentAsync(sourceSegment, targetSegment, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public IEnumerable<TranslationResult> GetCurrentResults()
        {
            return _wordGraphProcessor.GetResults();
        }

        private void Correct()
        {
            string prefix = _prefix.ToString();
            PrefixWordRanges = _targetTokenizer.TokenizeAsRanges(prefix).ToArray();
            IsLastWordComplete =
                PrefixWordRanges.Count == 0 || PrefixWordRanges[PrefixWordRanges.Count - 1].End < prefix.Length;
            _wordGraphProcessor.Correct(prefix.Split(PrefixWordRanges), IsLastWordComplete);
        }

        private IReadOnlyList<Range<int>> GetAlignedSourceSegment(TranslationResult result)
        {
            int sourceLength = 0;
            foreach (Phrase phrase in result.Phrases)
            {
                if (phrase.TargetSegmentCut > PrefixWordRanges.Count)
                    break;

                if (phrase.SourceSegmentRange.End > sourceLength)
                    sourceLength = phrase.SourceSegmentRange.End;
            }

            return sourceLength == SegmentWordRanges.Count
                ? SegmentWordRanges
                : SegmentWordRanges.Take(sourceLength).ToArray();
        }
    }
}
