import { TranslationResult } from './translation-result';
import { WordGraph } from './word-graph';

export class HybridInteractiveTranslationResult {
  constructor(public readonly smtWordGraph: WordGraph, public readonly ruleResult?: TranslationResult) {}
}
