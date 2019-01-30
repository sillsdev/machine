import { TranslationResult } from './translation-result';
import { TranslationSuggestion } from './translation-suggestion';

export interface TranslationSuggester {
  confidenceThreshold: number;
  getSuggestion(prefixCount: number, isLastWordComplete: boolean, result: TranslationResult): TranslationSuggestion;
}
