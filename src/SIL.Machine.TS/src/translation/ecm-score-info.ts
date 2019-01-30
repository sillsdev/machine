import { EditOperation } from './edit-operation';

export class EcmScoreInfo {
  readonly scores: number[] = [];
  readonly operations: EditOperation[] = [];

  updatePositions(prevEsi: EcmScoreInfo, positions: number[]): void {
    while (this.scores.length < prevEsi.scores.length) {
      this.scores.push(0);
    }

    while (this.operations.length < prevEsi.operations.length) {
      this.operations.push(EditOperation.None);
    }

    for (let i = 0; i < positions.length; i++) {
      this.scores[positions[i]] = prevEsi.scores[positions[i]];
      if (prevEsi.operations.length > i) {
        this.operations[positions[i]] = prevEsi.operations[positions[i]];
      }
    }
  }

  removeLast(): void {
    if (this.scores.length > 1) {
      this.scores.pop();
    }
    if (this.operations.length > 1) {
      this.operations.pop();
    }
  }

  getLastInsPrefixWordFromEsi(): number[] {
    const results = new Array<number>(this.operations.length);

    for (let j = this.operations.length - 1; j >= 0; j--) {
      switch (this.operations[j]) {
        case EditOperation.Hit:
          results[j] = j - 1;
          break;

        case EditOperation.Insert:
          let tj = j;
          while (tj >= 0 && this.operations[tj] === EditOperation.Insert) {
            tj--;
          }
          if (this.operations[tj] === EditOperation.Hit || this.operations[tj] === EditOperation.Substitute) {
            tj--;
          }
          results[j] = tj;
          break;

        case EditOperation.Delete:
          results[j] = j;
          break;

        case EditOperation.Substitute:
          results[j] = j - 1;
          break;

        case EditOperation.None:
          results[j] = 0;
          break;
      }
    }

    return results;
  }
}
