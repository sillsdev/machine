using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
    public class PhraseTranslationSuggester : ITranslationSuggester
    {
        public double ConfidenceThreshold { get; set; }
        public bool BreakOnPunctuation { get; set; } = true;

        public IReadOnlyList<TranslationSuggestion> GetSuggestions(
            int n,
            int prefixCount,
            bool isLastWordComplete,
            IEnumerable<TranslationResult> results
        )
        {
            var suggestions = new List<TranslationSuggestion>();
            foreach (TranslationResult result in results)
            {
                int startingJ = prefixCount;
                if (!isLastWordComplete)
                {
                    // if the prefix ends with a partial word and it has been completed,
                    // then make sure it is included as a suggestion,
                    // otherwise, don't return any suggestions
                    if ((result.Sources[startingJ - 1] & TranslationSources.Smt) != 0)
                        startingJ--;
                    else
                        break;
                }

                int k = 0;
                while (k < result.Phrases.Count && result.Phrases[k].TargetSegmentCut <= startingJ)
                    k++;

                double suggestionConfidence = -1;
                var indices = new List<int>();
                for (; k < result.Phrases.Count; k++)
                {
                    Phrase phrase = result.Phrases[k];
                    double phraseConfidence = 1;
                    double endingJ = startingJ;
                    for (int j = startingJ; j < phrase.TargetSegmentCut; j++)
                    {
                        if (result.Sources[j] == TranslationSources.None)
                        {
                            phraseConfidence = 0;
                            break;
                        }

                        string word = result.TargetTokens[j];
                        if (BreakOnPunctuation && word.All(char.IsPunctuation))
                            break;

                        phraseConfidence = Math.Min(phraseConfidence, result.Confidences[j]);
                        if (phraseConfidence < ConfidenceThreshold)
                            break;

                        endingJ = j + 1;
                    }

                    if (phraseConfidence >= ConfidenceThreshold)
                    {
                        suggestionConfidence =
                            suggestionConfidence == -1
                                ? phraseConfidence
                                : Math.Min(suggestionConfidence, phraseConfidence);

                        if (startingJ == endingJ)
                            break;

                        for (int j = startingJ; j < endingJ; j++)
                            indices.Add(j);

                        startingJ = phrase.TargetSegmentCut;
                    }
                    else
                    {
                        // hit a phrase with a low confidence, so don't include any more words in this suggestion
                        break;
                    }
                }
                if (suggestionConfidence == -1)
                    break;
                else if (indices.Count == 0)
                    continue;

                var newSuggestion = new TranslationSuggestion(result, indices, suggestionConfidence);
                bool duplicate = false;
                string[] newSuggestionWords = null;
                int[] table = null;
                foreach (TranslationSuggestion suggestion in suggestions)
                {
                    if (suggestion.TargetWordIndices.Count >= newSuggestion.TargetWordIndices.Count)
                    {
                        if (newSuggestionWords == null)
                            newSuggestionWords = newSuggestion.TargetWords.ToArray();
                        if (table == null)
                            table = ComputeKmpTable(newSuggestionWords);
                        if (IsSubsequence(table, newSuggestionWords, suggestion.TargetWords.ToArray()))
                        {
                            duplicate = true;
                            break;
                        }
                    }
                }

                if (!duplicate)
                {
                    suggestions.Add(newSuggestion);
                    if (suggestions.Count == n)
                        break;
                }
            }
            return suggestions;
        }

        private static bool IsSubsequence(
            int[] table,
            IReadOnlyList<string> newSuggestion,
            IReadOnlyList<string> suggestion
        )
        {
            int j = 0;
            int i = 0;
            while (i < suggestion.Count)
            {
                if (newSuggestion[j] == suggestion[i])
                {
                    j++;
                    i++;
                }
                if (j == newSuggestion.Count)
                {
                    return true;
                }
                else if (i < suggestion.Count && newSuggestion[j] != suggestion[i])
                {
                    if (j != 0)
                        j = table[j - 1];
                    else
                        i++;
                }
            }
            return false;
        }

        private static int[] ComputeKmpTable(IReadOnlyList<string> newSuggestion)
        {
            var table = new int[newSuggestion.Count];
            int len = 0;
            int i = 1;
            table[0] = 0;

            while (i < newSuggestion.Count)
            {
                if (newSuggestion[i] == newSuggestion[len])
                {
                    len++;
                    table[i] = len;
                    i++;
                }
                else if (len != 0)
                {
                    len = table[len - 1];
                }
                else
                {
                    table[i] = len;
                    i++;
                }
            }
            return table;
        }
    }
}
