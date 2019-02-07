import { TranslationResult } from './translation-result';

export interface InteractiveTranslationSession {
  readonly sourceSegment: string[];
  readonly prefix: string[];
  readonly isLastWordComplete: boolean;
  readonly currentResults: TranslationResult[];

  setPrefix(prefix: string[], isLastWordComplete: boolean): TranslationResult[];
  appendToPrefix(addition: string, isLastWordComplete: boolean): TranslationResult[];
  appendWordsToPrefix(words: string[]): TranslationResult[];
  approve(): Promise<void>;
}
