using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
    public class PhraseTranslationSuggester : ITranslationSuggester
    {
        public double ConfidenceThreshold { get; set; }

        public TranslationSuggestion GetSuggestion(int prefixCount, bool isLastWordComplete, TranslationResult result)
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
                    return new TranslationSuggestion(result);
            }

            int k = 0;
            while (k < result.Phrases.Count && result.Phrases[k].TargetSegmentCut <= startingJ)
                k++;

            double suggestionConfidence = -1;
            var indices = new List<int>();
            for (; k < result.Phrases.Count; k++)
            {
                Phrase phrase = result.Phrases[k];
                bool hitBreakingWord = false;
                double phraseConfidence = -1;
                double endingJ = startingJ;
                for (int j = startingJ; j < phrase.TargetSegmentCut; j++)
                {
                    string word = result.TargetTokens[j];
                    TranslationSources sources = result.Sources[j];
                    if (sources == TranslationSources.None || word.All(char.IsPunctuation))
                    {
                        hitBreakingWord = true;
                        break;
                    }

                    phraseConfidence = Math.Min(
                        phraseConfidence < 0 ? double.MaxValue : phraseConfidence,
                        result.Confidences[j]
                    );
                    if (phraseConfidence < ConfidenceThreshold)
                        break;

                    endingJ = j;
                }

                if (phraseConfidence >= ConfidenceThreshold)
                {
                    suggestionConfidence = Math.Min(
                        suggestionConfidence < 0 ? double.MaxValue : suggestionConfidence,
                        phraseConfidence
                    );

                    for (int j = startingJ; j <= endingJ; j++)
                        indices.Add(j);

                    startingJ = phrase.TargetSegmentCut;
                    if (hitBreakingWord)
                        break;
                }
                else
                {
                    break;
                }
            }

            return new TranslationSuggestion(result, indices, suggestionConfidence < 0 ? 0 : suggestionConfidence);
        }
    }
}
