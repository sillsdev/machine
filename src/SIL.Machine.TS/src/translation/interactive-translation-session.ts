import { Tokenizer } from '../tokenization/tokenizer';
import { WebApiClient } from '../web-api/web-api-client';
import { MAX_SEGMENT_LENGTH } from './constants';
import { ErrorCorrectionModel } from './error-correction-model';
import { ErrorCorrectionWordGraphProcessor } from './error-correction-word-graph-processor';
import { HybridInteractiveTranslationResult } from './hybrid-interactive-translation-result';
import { PhraseTranslationSuggester } from './phrase-translation-suggester';
import { TranslationResult } from './translation-result';
import { TranslationResultBuilder } from './translation-result-builder';
import { TranslationSuggester } from './translation-suggester';
import { WordGraph } from './word-graph';

const RULE_ENGINE_THRESHOLD: number = 0.05;

export class InteractiveTranslationSession {
  readonly smtWordGraph: WordGraph;
  readonly ruleResult?: TranslationResult;
  prefix: string[] = [];
  isLastWordComplete: boolean = false;
  suggestion: string[] = [];
  suggestionConfidence: number = 0;

  private wordGraphProcessor?: ErrorCorrectionWordGraphProcessor;
  private readonly suggester: TranslationSuggester = new PhraseTranslationSuggester();
  private curResult?: TranslationResult;

  constructor(
    private readonly webApiClient: WebApiClient,
    private readonly ecm: ErrorCorrectionModel,
    private readonly targetWordTokenizer: Tokenizer,
    private readonly projectId: string,
    public readonly sourceSegment: string[],
    confidenceThreshold: number,
    result: HybridInteractiveTranslationResult
  ) {
    this.suggester.confidenceThreshold = confidenceThreshold;
    this.smtWordGraph = result.smtWordGraph;
    this.ruleResult = result.ruleResult;
  }

  get confidenceThreshold(): number {
    return this.suggester.confidenceThreshold;
  }

  set confidenceThreshold(value: number) {
    if (this.suggester.confidenceThreshold !== value) {
      this.suggester.confidenceThreshold = value;
      this.updateSuggestion();
    }
  }

  get isInitialized(): boolean {
    return this.wordGraphProcessor != null;
  }

  get isSourceSegmentValid(): boolean {
    return this.sourceSegment.length <= MAX_SEGMENT_LENGTH;
  }

  initialize(): void {
    if (this.isInitialized) {
      return;
    }

    this.wordGraphProcessor = new ErrorCorrectionWordGraphProcessor(this.ecm, this.sourceSegment, this.smtWordGraph);
    this.updatePrefix('');
  }

  updatePrefix(prefix: string): string[] {
    if (this.wordGraphProcessor == null) {
      throw new Error('The session has not been initialized');
    }

    const tokenRanges = this.targetWordTokenizer.tokenize(prefix);
    this.prefix = tokenRanges.map(r => prefix.substring(r.start, r.end));
    this.isLastWordComplete = tokenRanges.length === 0 || tokenRanges[tokenRanges.length - 1].end !== prefix.length;

    const results = this.wordGraphProcessor.correct(this.prefix, this.isLastWordComplete, 1);
    let smtResult: TranslationResult;
    if (results.length === 0) {
      const builder = new TranslationResultBuilder();
      smtResult = builder.toResult(this.sourceSegment, this.prefix.length);
    } else {
      smtResult = results[0];
    }

    if (this.ruleResult == null) {
      this.curResult = smtResult;
    } else {
      let prefixCount = this.prefix.length;
      if (!this.isLastWordComplete) {
        prefixCount--;
      }

      this.curResult = smtResult.merge(prefixCount, RULE_ENGINE_THRESHOLD, this.ruleResult);
    }

    this.updateSuggestion();

    return this.suggestion;
  }

  getSuggestionText(suggestionIndex: number = -1): string {
    if (!this.isInitialized) {
      throw new Error('The session has not been initialized.');
    }

    const words = suggestionIndex === -1 ? this.suggestion : this.suggestion.slice(0, suggestionIndex + 1);
    // TODO: use detokenizer to build suggestion text
    const text = words.join(' ');

    if (this.isLastWordComplete) {
      return text;
    }

    const lastToken = this.prefix[this.prefix.length - 1];
    return text.substring(lastToken.length);
  }

  async approve(): Promise<void> {
    if (!this.isInitialized) {
      throw new Error('The session has not been initialized.');
    }

    if (!this.isSourceSegmentValid || this.prefix.length > MAX_SEGMENT_LENGTH) {
      return;
    }

    await this.webApiClient.trainSegmentPair(this.projectId, this.sourceSegment, this.prefix);
  }

  private updateSuggestion(): void {
    const curResult = this.curResult;
    if (curResult == null) {
      return;
    }

    const suggestion = this.suggester.getSuggestion(this.prefix.length, this.isLastWordComplete, curResult);
    this.suggestion = suggestion.targetWordIndices.map(j => curResult.targetSegment[j]);
    this.suggestionConfidence = suggestion.confidence;
  }
}
