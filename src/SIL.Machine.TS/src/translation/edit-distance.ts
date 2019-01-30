import { EditOperation } from './edit-operation';

export interface DistMatrixResult {
  cost: number;
  distMatrix: number[][];
}

export interface DistMatrixCellResult {
  cost: number;
  iPred: number;
  jPred: number;
  op: EditOperation;
}

export abstract class EditDistance<TSeq, TItem> {
  protected abstract getCount(seq: TSeq): number;
  protected abstract getItem(seq: TSeq, index: number): TItem;
  protected abstract getHitCost(x: TItem, y: TItem, isComplete: boolean): number;
  protected abstract getSubstitutionCost(x: TItem, y: TItem, isComplete: boolean): number;
  protected abstract getDeletionCost(x: TItem): number;
  protected abstract getInsertionCost(y: TItem): number;
  protected abstract isHit(x: TItem, y: TItem, isComplete: boolean): boolean;

  protected initDistMatrix(x: TSeq, y: TSeq): number[][] {
    const xCount = this.getCount(x);
    const yCount = this.getCount(y);
    const dim = Math.max(xCount, yCount);
    const distMatrix = new Array<number[]>(dim + 1);
    for (let i = 0; i < distMatrix.length; i++) {
      distMatrix[i] = new Array<number>(dim + 1);
    }
    return distMatrix;
  }

  protected computeDistMatrix(
    x: TSeq,
    y: TSeq,
    isLastItemComplete: boolean,
    usePrefixDelOp: boolean
  ): DistMatrixResult {
    const distMatrix = this.initDistMatrix(x, y);

    const xCount = this.getCount(x);
    const yCount = this.getCount(y);
    for (let i = 0; i <= xCount; i++) {
      for (let j = 0; j <= yCount; j++) {
        const cellResult = this.processDistMatrixCell(
          x,
          y,
          distMatrix,
          usePrefixDelOp,
          j !== yCount || isLastItemComplete,
          i,
          j
        );
        distMatrix[i][j] = cellResult.cost;
      }
    }

    return { cost: distMatrix[xCount][yCount], distMatrix };
  }

  protected processDistMatrixCell(
    x: TSeq,
    y: TSeq,
    distMatrix: number[][],
    usePrefixDelOp: boolean,
    isComplete: boolean,
    i: number,
    j: number
  ): DistMatrixCellResult {
    let op: EditOperation;
    let iPred: number;
    let jPred: number;
    let cost: number;
    if (i !== 0 && j !== 0) {
      const xItem = this.getItem(x, i - 1);
      const yItem = this.getItem(y, j - 1);
      let substCost;
      if (this.isHit(xItem, yItem, isComplete)) {
        substCost = this.getHitCost(xItem, yItem, isComplete);
        op = EditOperation.Hit;
      } else {
        substCost = this.getSubstitutionCost(xItem, yItem, isComplete);
        op = EditOperation.Substitute;
      }

      let opCost = distMatrix[i - 1][j - 1] + substCost;
      cost = opCost;
      iPred = i - 1;
      jPred = j - 1;

      const delCost = usePrefixDelOp && j === this.getCount(y) ? 0 : this.getDeletionCost(xItem);
      opCost = distMatrix[i - 1][j] + delCost;
      if (opCost < cost) {
        cost = opCost;
        iPred = i - 1;
        jPred = j;
        op = delCost === 0 ? EditOperation.PrefixDelete : EditOperation.Delete;
      }

      opCost = distMatrix[i][j - 1] + this.getInsertionCost(yItem);
      if (opCost < cost) {
        cost = opCost;
        iPred = i;
        jPred = j - 1;
        op = EditOperation.Insert;
      }
    } else if (i === 0 && j === 0) {
      iPred = 0;
      jPred = 0;
      op = EditOperation.None;
      cost = 0;
    } else if (i === 0) {
      iPred = 0;
      jPred = j - 1;
      op = EditOperation.Insert;
      cost = distMatrix[0][j - 1] + this.getInsertionCost(this.getItem(y, j - 1));
    } else {
      iPred = i - 1;
      jPred = 0;
      op = EditOperation.Delete;
      cost = distMatrix[i - 1][0] + this.getDeletionCost(this.getItem(x, i - 1));
    }
    return { cost, iPred, jPred, op };
  }

  protected getOperations(
    x: TSeq,
    y: TSeq,
    distMatrix: number[][],
    isLastItemComplete: boolean,
    usePrefixDelOp: boolean,
    i: number,
    j: number
  ): EditOperation[] {
    const yCount = this.getCount(y);
    const ops: EditOperation[] = [];
    while (i > 0 || j > 0) {
      const result = this.processDistMatrixCell(
        x,
        y,
        distMatrix,
        usePrefixDelOp,
        j !== yCount || isLastItemComplete,
        i,
        j
      );
      i = result.iPred;
      j = result.jPred;
      if (result.op !== EditOperation.PrefixDelete) {
        ops.unshift(result.op);
      }
    }
    return ops;
  }
}
