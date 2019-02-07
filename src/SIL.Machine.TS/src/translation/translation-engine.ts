import { TranslationResult } from './translation-result';

export interface TranslationEngine {
  translate(segment: string[]): Promise<TranslationResult>;
  translateNBest(n: number, segment: string[]): Promise<TranslationResult[]>;
}
