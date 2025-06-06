﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.Machine.Corpora
{
    public class AlignedWordPair : IEquatable<AlignedWordPair>
    {
        public static IReadOnlyCollection<AlignedWordPair> Parse(string alignments, bool invert = false)
        {
            TryParse(alignments, out IReadOnlyCollection<AlignedWordPair> alignedWordPairs);
            return alignedWordPairs;
        }

        public static bool TryParse(
            string alignments,
            out IReadOnlyCollection<AlignedWordPair> alignedWordPairs,
            bool invert = false
        )
        {
            var result = new List<AlignedWordPair>();
            foreach (string token in alignments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int dashIndex = token.IndexOf('-');
                int colonIndex = token.IndexOf(':', dashIndex + 1);
                int secondColonIndex = token.IndexOf(':', colonIndex + 1);

                double translationScore = -1;
                double alignmentScore = -1;

                //If both translationScore and alignmentScore are specified
                if (colonIndex > 0 && secondColonIndex > 0)
                {
                    if (
                        !double.TryParse(
                            token.Substring(secondColonIndex + 1, token.Length - (secondColonIndex + 1)),
                            out alignmentScore
                        )
                    )
                    {
                        alignedWordPairs = result;
                        return false;
                    }
                }

                if (
                    !TryParseIndex(token.Substring(0, dashIndex), out int i)
                    || !TryParseIndex(
                        token.Substring(
                            dashIndex + 1,
                            (colonIndex == -1 ? token.Length : colonIndex) - (dashIndex + 1)
                        ),
                        out int j
                    )
                    || (
                        colonIndex > 0
                        && !double.TryParse(
                            token.Substring(
                                colonIndex + 1,
                                (secondColonIndex == -1 ? token.Length : secondColonIndex) - (colonIndex + 1)
                            ),
                            out translationScore
                        )
                    )
                )
                {
                    alignedWordPairs = result;
                    return false;
                }

                result.Add(
                    invert
                        ? new AlignedWordPair(j, i)
                        {
                            TranslationScore = translationScore,
                            AlignmentScore = alignmentScore
                        }
                        : new AlignedWordPair(i, j)
                        {
                            TranslationScore = translationScore,
                            AlignmentScore = alignmentScore
                        }
                );
            }
            alignedWordPairs = result;
            return true;
        }

        public AlignedWordPair(int sourceIndex, int targetIndex)
        {
            SourceIndex = sourceIndex;
            TargetIndex = targetIndex;
        }

        public int SourceIndex { get; }
        public int TargetIndex { get; }
        public bool IsSure { get; set; } = true;
        public double TranslationScore { get; set; } = -1;
        public double AlignmentScore { get; set; } = -1;

        public AlignedWordPair Invert()
        {
            return new AlignedWordPair(TargetIndex, SourceIndex)
            {
                IsSure = IsSure,
                TranslationScore = TranslationScore,
                AlignmentScore = AlignmentScore
            };
        }

        public bool Equals(AlignedWordPair other)
        {
            return other != null && SourceIndex == other.SourceIndex && TargetIndex == other.TargetIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is AlignedWordPair other && Equals(other);
        }

        public override int GetHashCode()
        {
            int code = 23;
            code = code * 31 + SourceIndex.GetHashCode();
            code = code * 31 + TargetIndex.GetHashCode();
            return code;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            string sourceIndex = SourceIndex < 0 ? "NULL" : SourceIndex.ToString();
            string targetIndex = TargetIndex < 0 ? "NULL" : TargetIndex.ToString();
            sb.Append($"{sourceIndex}-{targetIndex}");
            if (TranslationScore >= 0)
            {
                sb.Append($":{TranslationScore:0.########}");
                if (AlignmentScore >= 0)
                    sb.Append($":{AlignmentScore:0.########}");
            }
            return sb.ToString();
        }

        private static bool TryParseIndex(string indexString, out int index)
        {
            if (indexString == "NULL")
            {
                index = -1;
                return true;
            }
            if (int.TryParse(indexString, out int indexNum))
            {
                index = indexNum;
                return true;
            }
            index = -1;
            return false;
        }
    }
}
