import { WebApiClient } from '../web-api/web-api-client';
import { MAX_SEGMENT_LENGTH } from './constants';
import { ErrorCorrectionModel } from './error-correction-model';
import { ErrorCorrectionWordGraphProcessor } from './error-correction-word-graph-processor';
import { HybridInteractiveTranslationResult } from './hybrid-interactive-translation-result';
import { InteractiveTranslationSession } from './interactive-translation-session';
import { TranslationResult } from './translation-result';
import { WordGraph } from './word-graph';

const RULE_ENGINE_THRESHOLD: number = 0.05;

function sequenceEqual(x: string[], y: string[]): boolean {
  if (x === y) {
    return true;
  }
  if (x.length !== y.length) {
    return false;
  }

  for (let i = 0; i < x.length; i++) {
    if (x[i] !== y[i]) {
      return false;
    }
  }
  return true;
}

export class RemoteInteractiveTranslationSession implements InteractiveTranslationSession {
  readonly prefix: string[] = [];

  private _currentResults: TranslationResult[] = [];
  private readonly smtWordGraph: WordGraph;
  private readonly ruleResult?: TranslationResult;
  private _isLastWordComplete: boolean = true;
  private readonly wordGraphProcessor: ErrorCorrectionWordGraphProcessor;

  constructor(
    private readonly webApiClient: WebApiClient,
    private readonly ecm: ErrorCorrectionModel,
    private readonly projectId: string,
    private readonly n: number,
    public readonly sourceSegment: string[],
    result: HybridInteractiveTranslationResult
  ) {
    this.smtWordGraph = result.smtWordGraph;
    this.ruleResult = result.ruleResult;
    this.wordGraphProcessor = new ErrorCorrectionWordGraphProcessor(this.ecm, this.sourceSegment, this.smtWordGraph);
    this.updateCurrentResults();
  }

  get isLastWordComplete(): boolean {
    return this._isLastWordComplete;
  }

  get currentResults(): TranslationResult[] {
    return this._currentResults;
  }

  get isSourceSegmentValid(): boolean {
    return this.sourceSegment.length <= MAX_SEGMENT_LENGTH;
  }

  setPrefix(prefix: string[], isLastWordComplete: boolean): TranslationResult[] {
    if (!sequenceEqual(this.prefix, prefix) || this._isLastWordComplete !== isLastWordComplete) {
      this.prefix.length = 0;
      this.prefix.push(...prefix);
      this._isLastWordComplete = isLastWordComplete;
      this.updateCurrentResults();
    }
    return this._currentResults;
  }

  appendToPrefix(addition: string, isLastWordComplete: boolean): TranslationResult[] {
    if (addition === '' && this._isLastWordComplete) {
      throw new Error('An empty string cannot be added to a prefix where the last word is complete.');
    }

    if (addition !== '' || this._isLastWordComplete !== isLastWordComplete) {
      if (this.isLastWordComplete) {
        this.prefix.push(addition);
      } else {
        this.prefix[this.prefix.length - 1] = this.prefix[this.prefix.length - 1] + addition;
      }
      this._isLastWordComplete = isLastWordComplete;
      this.updateCurrentResults();
    }
    return this._currentResults;
  }

  appendWordsToPrefix(words: string[]): TranslationResult[] {
    let updated = false;
    for (const word of words) {
      if (this._isLastWordComplete) {
        this.prefix.push(word);
      } else {
        this.prefix[this.prefix.length - 1] = word;
      }
      this._isLastWordComplete = true;
      updated = true;
    }
    if (updated) {
      this.updateCurrentResults();
    }
    return this._currentResults;
  }

  async approve(alignedOnly: boolean): Promise<void> {
    if (!this.isSourceSegmentValid || this.prefix.length > MAX_SEGMENT_LENGTH) {
      return;
    }

    let sourceSegment = this.sourceSegment;
    if (alignedOnly) {
      if (this._currentResults.length === 0) {
        return;
      }
      sourceSegment = this._currentResults[0].getAlignedSourceSegment(this.prefix.length);
    }

    if (sourceSegment.length > 0) {
      await this.webApiClient.trainSegmentPair(this.projectId, sourceSegment, this.prefix);
    }
  }

  private updateCurrentResults(): void {
    const smtResults = this.wordGraphProcessor.correct(this.prefix, this.isLastWordComplete, this.n);
    const ruleResult = this.ruleResult;
    if (ruleResult == null) {
      this._currentResults = smtResults;
    } else {
      let prefixCount = this.prefix.length;
      if (!this.isLastWordComplete) {
        prefixCount--;
      }
      this._currentResults = smtResults.map(r => r.merge(prefixCount, RULE_ENGINE_THRESHOLD, ruleResult));
    }
  }
}
