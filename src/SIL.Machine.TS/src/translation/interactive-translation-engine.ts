import { Observable } from 'rxjs';

import { LatinWordTokenizer } from '../tokenization/latin-word-tokenizer';
import { Tokenizer } from '../tokenization/tokenizer';
import { HttpClient } from '../web-api/http-client';
import { WebApiClient } from '../web-api/web-api-client';
import { MAX_SEGMENT_LENGTH } from './constants';
import { ErrorCorrectionModel } from './error-correction-model';
import { HybridInteractiveTranslationResult } from './hybrid-interactive-translation-result';
import { InteractiveTranslationSession } from './interactive-translation-session';
import { ProgressStatus } from './progress-status';
import { WordGraph } from './word-graph';

export class InteractiveTranslationEngine {
  readonly sourceWordTokenizer: Tokenizer;
  readonly targetWordTokenizer: Tokenizer;

  private readonly ecm: ErrorCorrectionModel = new ErrorCorrectionModel();
  private readonly webApiClient: WebApiClient;

  constructor(public readonly projectId: string, baseUrl: string = '', accessToken: string = '') {
    const wordTokenizer = new LatinWordTokenizer();
    this.sourceWordTokenizer = wordTokenizer;
    this.targetWordTokenizer = wordTokenizer;
    this.webApiClient = new WebApiClient(new HttpClient(baseUrl, accessToken));
  }

  async translateInteractively(
    sourceSegment: string,
    confidenceThreshold: number
  ): Promise<InteractiveTranslationSession> {
    const tokens = this.sourceWordTokenizer.tokenizeToStrings(sourceSegment);
    let results: HybridInteractiveTranslationResult;
    if (tokens.length > MAX_SEGMENT_LENGTH) {
      results = new HybridInteractiveTranslationResult(new WordGraph());
    } else {
      results = await this.webApiClient.translateInteractively(this.projectId, tokens);
    }
    return new InteractiveTranslationSession(
      this.webApiClient,
      this.ecm,
      this.targetWordTokenizer,
      this.projectId,
      tokens,
      confidenceThreshold,
      results
    );
  }

  train(): Observable<ProgressStatus> {
    return this.webApiClient.train(this.projectId);
  }

  startTraining(): Promise<void> {
    return this.webApiClient.startTraining(this.projectId);
  }

  listenForTrainingStatus(): Observable<ProgressStatus> {
    return this.webApiClient.listenForTrainingStatus(this.projectId);
  }

  getConfidence(): Promise<number> {
    return this.webApiClient.getEngineConfidence(this.projectId);
  }
}
