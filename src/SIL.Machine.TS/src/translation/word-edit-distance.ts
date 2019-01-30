import { EditDistance } from './edit-distance';
import { EditOperation } from './edit-operation';

export interface WordEditDistanceResult {
  cost: number;
  ops: EditOperation[];
}

export class WordEditDistance extends EditDistance<string, string> {
  hitCost: number = 0;
  insertionCost: number = 0;
  deletionCost: number = 0;
  substitutionCost: number = 0;

  compute(x: string, y: string): WordEditDistanceResult {
    const matrixResult = this.computeDistMatrix(x, y, true, false);
    const ops = this.getOperations(x, y, matrixResult.distMatrix, true, false, this.getCount(x), this.getCount(y));
    return { cost: matrixResult.cost, ops };
  }

  computePrefix(x: string, y: string, isLastItemComplete: boolean, usePrefixDelOp: boolean): WordEditDistanceResult {
    const matrixResult = this.computeDistMatrix(x, y, isLastItemComplete, usePrefixDelOp);
    const ops = this.getOperations(
      x,
      y,
      matrixResult.distMatrix,
      isLastItemComplete,
      usePrefixDelOp,
      this.getCount(x),
      this.getCount(y)
    );
    return { cost: matrixResult.cost, ops };
  }

  protected getCount(seq: string): number {
    return seq.length;
  }

  protected getItem(seq: string, index: number): string {
    return seq[index];
  }

  protected getHitCost(x: string, y: string, isComplete: boolean): number {
    return this.hitCost;
  }

  protected getSubstitutionCost(x: string, y: string, isComplete: boolean): number {
    return this.substitutionCost;
  }

  protected getDeletionCost(x: string): number {
    return this.deletionCost;
  }

  protected getInsertionCost(y: string): number {
    return this.insertionCost;
  }

  protected isHit(x: string, y: string, isComplete: boolean): boolean {
    return x === y;
  }
}
