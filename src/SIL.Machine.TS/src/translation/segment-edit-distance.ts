import { EditDistance } from './edit-distance';
import { EditOperation } from './edit-operation';
import { WordEditDistance, WordEditDistanceResult } from './word-edit-distance';

export interface SegmentEditDistanceResult {
  cost: number;
  wordOps: EditOperation[];
  charOps: EditOperation[];
}

export class SegmentEditDistance extends EditDistance<string[], string> {
  private readonly wordEditDistance: WordEditDistance = new WordEditDistance();

  get hitCost(): number {
    return this.wordEditDistance.hitCost;
  }

  set hitCost(value: number) {
    this.wordEditDistance.hitCost = value;
  }

  get substitutionCost(): number {
    return this.wordEditDistance.substitutionCost;
  }

  set substitutionCost(value: number) {
    this.wordEditDistance.substitutionCost = value;
  }

  get insertionCost(): number {
    return this.wordEditDistance.insertionCost;
  }

  set insertionCost(value: number) {
    this.wordEditDistance.insertionCost = value;
  }

  get deletionCost(): number {
    return this.wordEditDistance.deletionCost;
  }

  set deletionCost(value: number) {
    this.wordEditDistance.deletionCost = value;
  }

  compute(x: string[], y: string[]): number {
    const matrixResult = this.computeDistMatrix(x, y, true, false);
    return matrixResult.cost;
  }

  computePrefix(
    x: string[],
    y: string[],
    isLastItemComplete: boolean,
    usePrefixDelOp: boolean
  ): SegmentEditDistanceResult {
    const matrixResult = this.computeDistMatrix(x, y, isLastItemComplete, usePrefixDelOp);

    let charOps: EditOperation[] = [];
    let i = x.length;
    let j = y.length;
    const wordOps: EditOperation[] = [];
    while (i > 0 || j > 0) {
      const cellResult = this.processDistMatrixCell(
        x,
        y,
        matrixResult.distMatrix,
        usePrefixDelOp,
        j !== y.length || isLastItemComplete,
        i,
        j
      );
      i = cellResult.iPred;
      j = cellResult.jPred;
      if (cellResult.op !== EditOperation.PrefixDelete) {
        wordOps.unshift(cellResult.op);
      }

      if (j + 1 === y.length && !isLastItemComplete && cellResult.op === EditOperation.Hit) {
        const wordResult = this.wordEditDistance.computePrefix(x[i], y[y.length - 1], true, true);
        charOps = wordResult.ops;
      }
    }

    return { cost: matrixResult.cost, wordOps, charOps };
  }

  incrComputePrefixFirstRow(scores: number[], prevScores: number[], yIncr: string[]): void {
    if (scores !== prevScores) {
      scores.length = 0;
      scores.push(...prevScores);
    }

    const startPos = scores.length;
    for (let jIncr = 0; jIncr < yIncr.length; jIncr++) {
      const j = startPos + jIncr;
      if (j === 0) {
        scores.push(this.getInsertionCost(yIncr[jIncr]));
      } else {
        scores.push(scores[j - 1] + this.getInsertionCost(yIncr[jIncr]));
      }
    }
  }

  incrComputePrefix(
    scores: number[],
    prevScores: number[],
    xWord: string,
    yIncr: string[],
    isLastItemComplete: boolean
  ): EditOperation[] {
    const x: string[] = [xWord];
    const y = new Array<string>(prevScores.length - 1);
    for (let i = 0; i < yIncr.length; i++) {
      y[prevScores.length - yIncr.length - 1 + i] = yIncr[i];
    }

    const distMatrix = this.initDistMatrix(x, y);

    for (let j = 0; j < prevScores.length; j++) {
      distMatrix[0][j] = prevScores[j];
    }
    for (let j = 0; j < scores.length; j++) {
      distMatrix[1][j] = scores[j];
    }

    while (scores.length < prevScores.length) {
      scores.push(0);
    }

    const startPos = prevScores.length - yIncr.length;

    const ops: EditOperation[] = [];
    for (let jIncr = 0; jIncr < yIncr.length; jIncr++) {
      const j = startPos + jIncr;
      const cellResult = this.processDistMatrixCell(
        x,
        y,
        distMatrix,
        false,
        j !== y.length || isLastItemComplete,
        1,
        j
      );
      scores[j] = cellResult.cost;
      distMatrix[1][j] = cellResult.cost;
      ops.push(cellResult.op);
    }

    return ops;
  }

  protected getCount(seq: string[]): number {
    return seq.length;
  }

  protected getItem(seq: string[], index: number): string {
    return seq[index];
  }

  protected getHitCost(x: string, y: string, isComplete: boolean): number {
    return this.hitCost * y.length;
  }

  protected getSubstitutionCost(x: string, y: string, isComplete: boolean): number {
    if (x === '') {
      return this.substitutionCost * 0.99 * y.length;
    }

    let result: WordEditDistanceResult;
    if (isComplete) {
      result = this.wordEditDistance.compute(x, y);
    } else {
      result = this.wordEditDistance.computePrefix(x, y, true, true);
    }

    const opCounts = this.getOpCounts(result.ops);

    return (
      this.hitCost * opCounts.hitCount +
      this.insertionCost * opCounts.insCount +
      this.substitutionCost * opCounts.substCount +
      this.deletionCost * opCounts.delCount
    );
  }

  protected getDeletionCost(x: string): number {
    if (x === '') {
      return this.deletionCost;
    }
    return this.deletionCost * x.length;
  }

  protected getInsertionCost(y: string): number {
    return this.insertionCost * y.length;
  }

  protected isHit(x: string, y: string, isComplete: boolean): boolean {
    return x === y || (!isComplete && x.startsWith(y));
  }

  private getOpCounts(
    ops: EditOperation[]
  ): { hitCount: number; insCount: number; substCount: number; delCount: number } {
    let hitCount = 0;
    let insCount = 0;
    let substCount = 0;
    let delCount = 0;
    for (const op of ops) {
      switch (op) {
        case EditOperation.Hit:
          hitCount++;
          break;
        case EditOperation.Insert:
          insCount++;
          break;
        case EditOperation.Substitute:
          substCount++;
          break;
        case EditOperation.Delete:
          delCount++;
          break;
      }
    }
    return { hitCount, insCount, substCount, delCount };
  }
}
