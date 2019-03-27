import { Observable } from 'rxjs';
import { HttpClient } from '../web-api/http-client';
import { WebApiClient } from '../web-api/web-api-client';
import { MAX_SEGMENT_LENGTH } from './constants';
import { ErrorCorrectionModel } from './error-correction-model';
import { HybridInteractiveTranslationResult } from './hybrid-interactive-translation-result';
import { InteractiveTranslationEngine } from './interactive-translation-engine';
import { InteractiveTranslationSession } from './interactive-translation-session';
import { ProgressStatus } from './progress-status';
import { RemoteInteractiveTranslationSession } from './remote-interactive-translation-session';
import { TranslationEngineStats } from './translation-engine-stats';
import { TranslationResult } from './translation-result';
import { TranslationResultBuilder } from './translation-result-builder';
import { WordGraph } from './word-graph';

export class RemoteTranslationEngine implements InteractiveTranslationEngine {
  private readonly ecm: ErrorCorrectionModel = new ErrorCorrectionModel();
  private readonly webApiClient: WebApiClient;

  constructor(public readonly projectId: string, httpClient: HttpClient) {
    this.webApiClient = new WebApiClient(httpClient);
  }

  async translate(segment: string[]): Promise<TranslationResult> {
    if (segment.length > MAX_SEGMENT_LENGTH) {
      const builder = new TranslationResultBuilder();
      return builder.toResult(segment);
    }
    return await this.webApiClient.translate(this.projectId, segment);
  }

  async translateNBest(n: number, segment: string[]): Promise<TranslationResult[]> {
    if (segment.length > MAX_SEGMENT_LENGTH) {
      return [];
    }
    return await this.webApiClient.translateNBest(this.projectId, n, segment);
  }

  async translateInteractively(n: number, segment: string[]): Promise<InteractiveTranslationSession> {
    let results: HybridInteractiveTranslationResult;
    if (segment.length > MAX_SEGMENT_LENGTH) {
      results = new HybridInteractiveTranslationResult(new WordGraph());
    } else {
      results = await this.webApiClient.translateInteractively(this.projectId, segment);
    }
    return new RemoteInteractiveTranslationSession(this.webApiClient, this.ecm, this.projectId, n, segment, results);
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

  getStats(): Promise<TranslationEngineStats> {
    return this.webApiClient.getEngineStats(this.projectId);
  }
}
