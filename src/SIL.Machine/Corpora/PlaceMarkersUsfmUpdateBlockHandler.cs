using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Translation;

namespace SIL.Machine.Corpora
{
    public class PlaceMarkersAlignmentInfo
    {
        public IReadOnlyList<string> Refs { get; }
        public IReadOnlyList<string> SourceTokens { get; }
        public IReadOnlyList<string> TranslationTokens { get; }
        public WordAlignmentMatrix Alignment { get; }

        public PlaceMarkersAlignmentInfo(
            IReadOnlyList<string> refs,
            IReadOnlyList<string> sourceTokens,
            IReadOnlyList<string> translationTokens,
            WordAlignmentMatrix alignment
        )
        {
            Refs = refs;
            SourceTokens = sourceTokens;
            TranslationTokens = translationTokens;
            Alignment = alignment;
        }
    }

    public class PlaceMarkersUsfmUpdateBlockHandler : IUsfmUpdateBlockHandler
    {
        private readonly IDictionary<string, PlaceMarkersAlignmentInfo> _alignmentInfo;

        public PlaceMarkersUsfmUpdateBlockHandler(IEnumerable<PlaceMarkersAlignmentInfo> alignmentInfo)
        {
            _alignmentInfo = alignmentInfo.ToDictionary(info => info.Refs.First(), info => info);
        }

        public UsfmUpdateBlock ProcessBlock(UsfmUpdateBlock block)
        {
            string reference = block.Refs.FirstOrDefault().ToString();
            List<UsfmUpdateBlockElement> elements = block.Elements.ToList();

            // Nothing to do if there are no markers to place or no alignment to use
            if (
                elements.Count == 0
                || !_alignmentInfo.TryGetValue(reference, out PlaceMarkersAlignmentInfo alignmentInfo)
                || alignmentInfo.Alignment.RowCount == 0
                || alignmentInfo.Alignment.ColumnCount == 0
                || !elements.Any(e =>
                    e.Type.IsOneOf(UsfmUpdateBlockElementType.Paragraph, UsfmUpdateBlockElementType.Style)
                    && !e.MarkedForRemoval
                )
            )
            {
                return block;
            }

            // Paragraph markers at the end of the block should stay there
            // Section headers should be ignored but re-inserted in the same position relative to other paragraph markers
            List<UsfmUpdateBlockElement> endElements = new List<UsfmUpdateBlockElement>();
            bool eobEmptyParas = true;
            List<(int ParaMarkersLeft, UsfmUpdateBlockElement Element)> headerElements =
                new List<(int paraMarkersLeft, UsfmUpdateBlockElement element)>();
            int paraMarkersLeft = 0;
            foreach ((int i, UsfmUpdateBlockElement element) in elements.Select((e, i) => (i, e)).Reverse())
            {
                if (element.Type == UsfmUpdateBlockElementType.Paragraph && !element.MarkedForRemoval)
                {
                    if (element.Tokens.Count > 1)
                    {
                        headerElements.Insert(0, (paraMarkersLeft, element));
                        elements.RemoveAt(i);
                    }
                    else
                    {
                        paraMarkersLeft++;

                        if (eobEmptyParas)
                        {
                            endElements.Insert(0, element);
                            elements.RemoveAt(i);
                        }
                    }
                }
                else if (
                    !(
                        element.Type == UsfmUpdateBlockElementType.Embed
                        || (
                            element.Type == UsfmUpdateBlockElementType.Text
                            && element.Tokens[0].ToUsfm().Trim().Count() == 0
                        )
                    )
                )
                {
                    eobEmptyParas = false;
                }
            }

            IReadOnlyList<string> sourceTokens = alignmentInfo.SourceTokens;
            IReadOnlyList<string> targetTokens = alignmentInfo.TranslationTokens;
            int sourceTokenIndex = 0;

            string sourceSentence = "";
            string targetSentence = "";
            List<UsfmUpdateBlockElement> toPlace = new List<UsfmUpdateBlockElement>();
            List<int> adjacentSourceTokens = new List<int>();
            List<UsfmUpdateBlockElement> placedElements = new List<UsfmUpdateBlockElement>();
            if (elements[0].Type == UsfmUpdateBlockElementType.Other)
            {
                placedElements.Add(elements[0]);
                elements.RemoveAt(0);
            }
            List<UsfmUpdateBlockElement> embedElements = new List<UsfmUpdateBlockElement>();
            List<UsfmUpdateBlockElement> ignoredElements = new List<UsfmUpdateBlockElement>();
            foreach (UsfmUpdateBlockElement element in elements)
            {
                if (element.Type == UsfmUpdateBlockElementType.Text)
                {
                    if (element.MarkedForRemoval)
                    {
                        string text = element.Tokens[0].ToUsfm();
                        sourceSentence += text;

                        // Track seen tokens
                        while (sourceTokenIndex < sourceTokens.Count && text.Contains(sourceTokens[sourceTokenIndex]))
                        {
                            text = text.Substring(
                                text.IndexOf(sourceTokens[sourceTokenIndex]) + sourceTokens[sourceTokenIndex].Length
                            );
                            sourceTokenIndex++;
                        }
                        // Handle tokens split across text elements
                        if (text.Trim().Length > 0)
                            sourceTokenIndex++;
                    }
                    else
                    {
                        targetSentence += element.Tokens[0].ToUsfm();
                    }
                }

                if (element.MarkedForRemoval)
                {
                    ignoredElements.Add(element);
                }
                else if (element.Type == UsfmUpdateBlockElementType.Embed)
                {
                    embedElements.Add(element);
                }
                else if (element.Type.IsOneOf(UsfmUpdateBlockElementType.Paragraph, UsfmUpdateBlockElementType.Style))
                {
                    toPlace.Add(element);
                    adjacentSourceTokens.Add(sourceTokenIndex);
                }
            }

            List<int> targetTokenStarts = new List<int>();
            int prevLength = 0;
            foreach (string token in targetTokens)
            {
                targetTokenStarts.Add(targetSentence.IndexOf(token, targetTokenStarts.LastOrDefault() + prevLength));
                prevLength = token.Length;
            }

            List<(int Index, UsfmUpdateBlockElement Element)> toInsert =
                new List<(int Index, UsfmUpdateBlockElement Element)>();
            foreach (
                (UsfmUpdateBlockElement element, int adjacentSourceToken) in toPlace
                    .Zip(adjacentSourceTokens)
                    .Select(tuple => (tuple.Item1, tuple.Item2))
            )
            {
                int adjacentTargetToken = PredictMarkerLocation(
                    alignmentInfo.Alignment,
                    adjacentSourceToken,
                    sourceTokens,
                    targetTokens
                );
                int targetStringIndex =
                    adjacentTargetToken < targetTokenStarts.Count
                        ? targetTokenStarts[adjacentTargetToken]
                        : targetSentence.Length;
                toInsert.Add((targetStringIndex, element));
            }
            toInsert.Sort((p1, p2) => p1.Index.CompareTo(p2.Index));
            toInsert.AddRange(embedElements.Concat(endElements).Select(e => (targetSentence.Length, e)));

            // Construct new text tokens to put between markers
            // and reincorporate headers and empty end-of-verse paragraph markers
            if (toInsert[0].Index > 0)
            {
                placedElements.Add(
                    new UsfmUpdateBlockElement(
                        UsfmUpdateBlockElementType.Text,
                        new List<UsfmToken>() { new UsfmToken(targetSentence.Substring(0, toInsert[0].Index)) }
                    )
                );
            }

            foreach ((int j, (int insertIndex, UsfmUpdateBlockElement element)) in toInsert.Select((p, i) => (i, p)))
            {
                if (element.Type == UsfmUpdateBlockElementType.Paragraph)
                {
                    while (headerElements.Count > 0 && headerElements[0].ParaMarkersLeft == paraMarkersLeft)
                    {
                        placedElements.Add(headerElements[0].Element);
                        headerElements.RemoveAt(0);
                    }
                    paraMarkersLeft--;
                }

                placedElements.Add(element);
                if (
                    insertIndex < targetSentence.Length
                    && (j + 1 == toInsert.Count || insertIndex < toInsert[j + 1].Index)
                )
                {
                    UsfmToken textToken;
                    if (j + 1 < toInsert.Count)
                    {
                        textToken = new UsfmToken(
                            targetSentence.Substring(insertIndex, toInsert[j + 1].Index - insertIndex)
                        );
                    }
                    else
                    {
                        textToken = new UsfmToken(targetSentence.Substring(insertIndex));
                    }
                    placedElements.Add(
                        new UsfmUpdateBlockElement(UsfmUpdateBlockElementType.Text, new List<UsfmToken> { textToken })
                    );
                }
            }
            while (headerElements.Count > 0)
            {
                placedElements.Add(headerElements[0].Element);
                headerElements.RemoveAt(0);
            }

            UsfmUpdateBlock processedBlock = new UsfmUpdateBlock(
                refs: block.Refs,
                elements: placedElements.Concat(ignoredElements)
            );
            return processedBlock;
        }

        private int PredictMarkerLocation(
            WordAlignmentMatrix alignment,
            int adjacentSourceToken,
            IReadOnlyList<string> sourceTokens,
            IReadOnlyList<string> targetTokens
        )
        {
            // Gets the number of alignment pairs that "cross the line" between
            // the src marker position and the potential trg marker position, (src_idx - .5) and (trg_idx - .5)
            int NumAlignCrossings(int sourceIndex, int targetIndex)
            {
                int crossings = 0;
                for (int i = 0; i < alignment.RowCount; i++)
                {
                    for (int j = 0; j < alignment.ColumnCount; j++)
                    {
                        if (
                            alignment[i, j]
                            && ((i < sourceIndex && j >= targetIndex) || (i >= sourceIndex && j < targetIndex))
                        )
                        {
                            crossings++;
                        }
                    }
                }
                return crossings;
            }

            // If the token on either side of a potential target location is punctuation,
            // use it as the basis for deciding the target marker location
            int targetHypothesis = -1;
            int[] punctuationHypotheses = new int[] { -1, 0 };
            foreach (int punctuationHypothesis in punctuationHypotheses)
            {
                int sourceHypothesis = adjacentSourceToken + punctuationHypothesis;
                if (sourceHypothesis < 0 || sourceHypothesis >= sourceTokens.Count)
                {
                    continue;
                }
                // Only accept aligned pairs where both the src and trg token are punctuation
                string hypothesisToken = sourceTokens[sourceHypothesis];
                if (
                    hypothesisToken.Length > 0
                    && !hypothesisToken.Any(char.IsLetter)
                    && sourceHypothesis < alignment.RowCount
                )
                {
                    List<int> alignedTargetTokens = alignment.GetRowAlignedIndices(sourceHypothesis).ToList();
                    // If aligning to a token that precedes that marker,
                    // the trg token predicted to be closest to the marker
                    // is the last token aligned to the src rather than the first
                    if (punctuationHypothesis < 0)
                        alignedTargetTokens.Reverse();
                    foreach (int targetIndex in alignedTargetTokens)
                    {
                        string targetToken = targetTokens[targetIndex];
                        if (targetToken.Length > 0 && !targetToken.Any(char.IsLetter))
                        {
                            targetHypothesis = targetIndex;
                            break;
                        }
                    }
                }
                if (targetHypothesis != -1)
                {
                    // Since the marker location is represented by the token after the marker,
                    // adjust the index when aligning to punctuation that precedes the token
                    return targetHypothesis + (punctuationHypothesis == -1 ? 1 : 0);
                }
            }

            int[] hypotheses = new int[] { 0, 1, 2 };
            int bestHypothesis = -1;
            int bestNumCrossings = 200 ^ 2;
            HashSet<int> checkedHypotheses = new HashSet<int>();
            foreach (int hypothesis in hypotheses)
            {
                int sourceHypothesis = adjacentSourceToken + hypothesis;
                if (checkedHypotheses.Contains(sourceHypothesis))
                    continue;
                targetHypothesis = -1;
                while (targetHypothesis == -1 && sourceHypothesis >= 0 && sourceHypothesis < alignment.RowCount)
                {
                    checkedHypotheses.Add(sourceHypothesis);
                    List<int> alignedTargetTokens = alignment.GetRowAlignedIndices(sourceHypothesis).ToList();
                    if (alignedTargetTokens.Count > 0)
                    {
                        // If aligning with a source token that precedes the marker,
                        // the target token predicted to be closest to the marker is the last aligned token rather than the first
                        targetHypothesis = alignedTargetTokens[hypothesis < 0 ? -1 : 0];
                    }
                    else
                    {
                        // continue the search outwards
                        sourceHypothesis += hypothesis < 0 ? -1 : 1;
                    }
                }
                if (targetHypothesis != -1)
                {
                    int numCrossings = NumAlignCrossings(adjacentSourceToken, targetHypothesis);
                    if (numCrossings < bestNumCrossings)
                    {
                        bestHypothesis = targetHypothesis;
                        bestNumCrossings = numCrossings;
                    }
                    if (numCrossings == 0)
                    {
                        break;
                    }
                }
            }

            // If no alignments found, insert at the end of the sentence
            return bestHypothesis != -1 ? bestHypothesis : targetTokens.Count;
        }
    }
}
