export class TranslationSuggestion {
  constructor(public readonly targetWordIndices: number[] = [], public readonly confidence: number = 0) {}
}
