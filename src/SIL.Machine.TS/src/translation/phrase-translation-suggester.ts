import XRegExp from 'xregexp';

import { TranslationResult } from './translation-result';
import { TranslationSources } from './translation-sources';
import { TranslationSuggester } from './translation-suggester';
import { TranslationSuggestion } from './translation-suggestion';

const ALL_PUNCT_REGEXP = XRegExp('^\\p{P}*$');

export class PhraseTranslationSuggester implements TranslationSuggester {
  confidenceThreshold: number = 0;

  getSuggestion(prefixCount: number, isLastWordComplete: boolean, result: TranslationResult): TranslationSuggestion {
    let startingJ = prefixCount;
    if (!isLastWordComplete) {
      // if the prefix ends with a partial word and it has been completed,
      // then make sure it is included as a suggestion,
      // otherwise, don't return any suggestions
      if ((result.wordSources[startingJ - 1] & TranslationSources.Smt) !== 0) {
        startingJ--;
      } else {
        return new TranslationSuggestion();
      }
    }

    let k = 0;
    while (k < result.phrases.length && result.phrases[k].targetSegmentCut <= startingJ) {
      k++;
    }

    let minConfidence = -1;
    const indices: number[] = [];
    for (; k < result.phrases.length; k++) {
      const phrase = result.phrases[k];
      if (phrase.confidence >= this.confidenceThreshold) {
        let hitBreakingWord = false;
        for (let j = startingJ; j < phrase.targetSegmentCut; j++) {
          const word = result.targetSegment[j];
          const sources = result.wordSources[j];
          if (sources === TranslationSources.None || ALL_PUNCT_REGEXP.test(word)) {
            hitBreakingWord = true;
            break;
          }
          indices.push(j);
        }
        if (minConfidence < 0 || phrase.confidence < minConfidence) {
          minConfidence = phrase.confidence;
        }
        startingJ = phrase.targetSegmentCut;
        if (hitBreakingWord) {
          break;
        }
      } else {
        break;
      }
    }

    return new TranslationSuggestion(indices, minConfidence < 0 ? 0 : minConfidence);
  }
}
