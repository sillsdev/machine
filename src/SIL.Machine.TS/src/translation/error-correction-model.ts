import { EcmScoreInfo } from './ecm-score-info';
import { EditOperation } from './edit-operation';
import { SegmentEditDistance } from './segment-edit-distance';
import { TranslationResultBuilder } from './translation-result-builder';

export class ErrorCorrectionModel {
  private readonly segmentEditDistance: SegmentEditDistance = new SegmentEditDistance();

  constructor() {
    this.setErrorModelParameters(128, 0.8, 1, 1, 1);
  }

  setErrorModelParameters(
    vocSize: number,
    hitProb: number,
    insFactor: number,
    substFactor: number,
    delFactor: number
  ): void {
    let e: number;
    if (vocSize === 0) {
      e = (1 - hitProb) / (insFactor + substFactor + delFactor);
    } else {
      e = (1 - hitProb) / (insFactor * vocSize + substFactor * (vocSize - 1) + delFactor);
    }

    const insProb = e * insFactor;
    const substProb = e * substFactor;
    const delProb = e * delFactor;

    this.segmentEditDistance.hitCost = -Math.log(hitProb);
    this.segmentEditDistance.insertionCost = -Math.log(insProb);
    this.segmentEditDistance.substitutionCost = -Math.log(substProb);
    this.segmentEditDistance.deletionCost = -Math.log(delProb);
  }

  setupInitialEsi(initialEsi: EcmScoreInfo): void {
    const score = this.segmentEditDistance.compute([], []);
    initialEsi.scores.length = 0;
    initialEsi.scores.push(score);
    initialEsi.operations.length = 0;
  }

  setupEsi(esi: EcmScoreInfo, prevEsi: EcmScoreInfo, word: string): void {
    const score = this.segmentEditDistance.compute([word], []);
    esi.scores.length = 0;
    esi.scores.push(prevEsi.scores[0] + score);
    esi.operations.length = 0;
    esi.operations.push(EditOperation.None);
  }

  extendInitialEsi(initialEsi: EcmScoreInfo, prevInitialEsi: EcmScoreInfo, prefixDif: string[]): void {
    this.segmentEditDistance.incrComputePrefixFirstRow(initialEsi.scores, prevInitialEsi.scores, prefixDif);
  }

  extendEsi(
    esi: EcmScoreInfo,
    prevEsi: EcmScoreInfo,
    word: string,
    prefixDif: string[],
    isLastWordComplete: boolean
  ): void {
    const ops = this.segmentEditDistance.incrComputePrefix(
      esi.scores,
      prevEsi.scores,
      word,
      prefixDif,
      isLastWordComplete
    );
    esi.operations.push(...ops);
  }

  correctPrefix(
    builder: TranslationResultBuilder,
    uncorrectedPrefixLen: number,
    prefix: string[],
    isLastWordComplete: boolean
  ): number {
    if (uncorrectedPrefixLen === 0) {
      for (const w of prefix) {
        builder.appendWord(w);
      }
      return prefix.length;
    }

    const result = this.segmentEditDistance.computePrefix(
      builder.words.slice(0, uncorrectedPrefixLen),
      prefix,
      isLastWordComplete,
      false
    );
    return builder.correctPrefix(result.wordOps, result.charOps, prefix, isLastWordComplete);
  }
}
