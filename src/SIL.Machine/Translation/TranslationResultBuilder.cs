﻿using System.Collections.Generic;
using System.Text;
using SIL.Machine.Annotations;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Translation
{
    public class TranslationResultBuilder
    {
        private readonly List<string> _targetTokens;
        private readonly List<double> _confidences;
        private readonly List<TranslationSources> _sources;
        private readonly List<PhraseInfo> _phrases;

        public TranslationResultBuilder(IReadOnlyList<string> sourceTokens)
        {
            SourceTokens = sourceTokens;
            _targetTokens = new List<string>();
            _confidences = new List<double>();
            _sources = new List<TranslationSources>();
            _phrases = new List<PhraseInfo>();
        }

        public IReadOnlyList<string> SourceTokens { get; }

        public IDetokenizer<string, string> TargetDetokenizer { get; set; } = WhitespaceDetokenizer.Instance;

        public IReadOnlyList<string> TargetTokens => _targetTokens;
        public IReadOnlyList<double> Confidences => _confidences;
        public IReadOnlyList<TranslationSources> Sources => _sources;
        public IReadOnlyList<PhraseInfo> Phrases => _phrases;

        public void AppendToken(string token, TranslationSources source, double confidence)
        {
            _targetTokens.Add(token);
            _sources.Add(source);
            _confidences.Add(confidence);
        }

        public void MarkPhrase(Range<int> sourceSegmentRange, WordAlignmentMatrix alignment)
        {
            _phrases.Add(new PhraseInfo(sourceSegmentRange, _targetTokens.Count, alignment));
        }

        public void SetConfidence(int index, double confidence)
        {
            _confidences[index] = confidence;
        }

        public int CorrectPrefix(
            IEnumerable<EditOperation> wordOps,
            IEnumerable<EditOperation> charOps,
            string[] prefix,
            bool isLastWordComplete
        )
        {
            var alignmentColsToCopy = new List<int>();

            int i = 0,
                j = 0,
                k = 0;
            foreach (EditOperation wordOp in wordOps)
            {
                switch (wordOp)
                {
                    case EditOperation.Insert:
                        _targetTokens.Insert(j, prefix[j]);
                        _sources.Insert(j, TranslationSources.Prefix);
                        _confidences.Insert(j, -1);
                        alignmentColsToCopy.Add(-1);
                        for (int l = k; l < _phrases.Count; l++)
                            _phrases[l].TargetCut++;
                        j++;
                        break;

                    case EditOperation.Delete:
                        _targetTokens.RemoveAt(j);
                        _sources.RemoveAt(j);
                        _confidences.RemoveAt(j);
                        i++;
                        if (k < _phrases.Count)
                        {
                            for (int l = k; l < _phrases.Count; l++)
                                _phrases[l].TargetCut--;

                            if (
                                _phrases[k].TargetCut <= 0
                                || (k > 0 && _phrases[k].TargetCut == _phrases[k - 1].TargetCut)
                            )
                            {
                                _phrases.RemoveAt(k);
                                alignmentColsToCopy.Clear();
                                i = 0;
                            }
                            else if (j >= _phrases[k].TargetCut)
                            {
                                ResizeAlignment(k, alignmentColsToCopy);
                                alignmentColsToCopy.Clear();
                                i = 0;
                                k++;
                            }
                        }
                        break;

                    case EditOperation.Hit:
                    case EditOperation.Substitute:
                        if (wordOp == EditOperation.Substitute || j < prefix.Length - 1 || isLastWordComplete)
                            _targetTokens[j] = prefix[j];
                        else
                            _targetTokens[j] = CorrectWord(charOps, _targetTokens[j], prefix[j]);

                        if (wordOp == EditOperation.Substitute)
                        {
                            _confidences[j] = -1;
                            _sources[j] = TranslationSources.Prefix;
                        }
                        else if (wordOp == EditOperation.Hit)
                            _sources[j] |= TranslationSources.Prefix;

                        alignmentColsToCopy.Add(i);

                        i++;
                        j++;
                        if (k < _phrases.Count && j >= _phrases[k].TargetCut)
                        {
                            ResizeAlignment(k, alignmentColsToCopy);
                            alignmentColsToCopy.Clear();
                            i = 0;
                            k++;
                        }
                        break;
                }
            }

            while (j < TargetTokens.Count)
            {
                alignmentColsToCopy.Add(i);

                i++;
                j++;
                if (k < _phrases.Count && j >= _phrases[k].TargetCut)
                {
                    ResizeAlignment(k, alignmentColsToCopy);
                    alignmentColsToCopy.Clear();
                    break;
                }
            }

            return alignmentColsToCopy.Count;
        }

        private void ResizeAlignment(int phraseIndex, List<int> colsToCopy)
        {
            WordAlignmentMatrix curAlignment = _phrases[phraseIndex].Alignment;
            if (colsToCopy.Count == curAlignment.ColumnCount)
                return;

            var newAlignment = new WordAlignmentMatrix(curAlignment.RowCount, colsToCopy.Count);
            for (int j = 0; j < newAlignment.ColumnCount; j++)
            {
                if (colsToCopy[j] != -1)
                {
                    for (int i = 0; i < newAlignment.RowCount; i++)
                        newAlignment[i, j] = curAlignment[i, colsToCopy[j]];
                }
            }

            _phrases[phraseIndex].Alignment = newAlignment;
        }

        private string CorrectWord(IEnumerable<EditOperation> charOps, string word, string prefix)
        {
            var sb = new StringBuilder();
            int i = 0,
                j = 0;
            foreach (EditOperation charOp in charOps)
            {
                switch (charOp)
                {
                    case EditOperation.Hit:
                        sb.Append(word[i]);
                        i++;
                        j++;
                        break;

                    case EditOperation.Insert:
                        sb.Append(prefix[j]);
                        j++;
                        break;

                    case EditOperation.Delete:
                        i++;
                        break;

                    case EditOperation.Substitute:
                        sb.Append(prefix[j]);
                        i++;
                        j++;
                        break;
                }
            }

            sb.Append(word.Substring(i));
            return sb.ToString();
        }

        public void Reset()
        {
            _targetTokens.Clear();
            _confidences.Clear();
            _sources.Clear();
            _phrases.Clear();
        }

        public TranslationResult ToResult(string translation = null)
        {
            var sources = new TranslationSources[TargetTokens.Count];
            var alignment = new WordAlignmentMatrix(SourceTokens.Count, _targetTokens.Count);
            var phrases = new List<Phrase>();
            int trgPhraseStartIndex = 0;
            foreach (PhraseInfo phraseInfo in _phrases)
            {
                for (int j = trgPhraseStartIndex; j < phraseInfo.TargetCut; j++)
                {
                    for (int i = phraseInfo.SourceSegmentRange.Start; i < phraseInfo.SourceSegmentRange.End; i++)
                    {
                        bool aligned = phraseInfo.Alignment[
                            i - phraseInfo.SourceSegmentRange.Start,
                            j - trgPhraseStartIndex
                        ];
                        if (aligned)
                            alignment[i, j] = true;
                    }

                    sources[j] = _sources[j];
                }

                phrases.Add(new Phrase(phraseInfo.SourceSegmentRange, phraseInfo.TargetCut));
                trgPhraseStartIndex = phraseInfo.TargetCut;
            }

            return new TranslationResult(
                translation ?? TargetDetokenizer.Detokenize(TargetTokens),
                SourceTokens,
                _targetTokens,
                _confidences,
                sources,
                alignment,
                phrases
            );
        }

        public class PhraseInfo
        {
            public PhraseInfo(Range<int> sourceSegmentRange, int targetCut, WordAlignmentMatrix alignment)
            {
                SourceSegmentRange = sourceSegmentRange;
                TargetCut = targetCut;
                Alignment = alignment;
            }

            public Range<int> SourceSegmentRange { get; }
            public int TargetCut { get; internal set; }
            public WordAlignmentMatrix Alignment { get; internal set; }
        }
    }
}
