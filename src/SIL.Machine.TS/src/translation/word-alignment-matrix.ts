export class WordAlignmentMatrix {
  private matrix: boolean[][] = [];

  constructor(public readonly rowCount: number, public readonly columnCount: number) {
    this.setAll(false);
  }

  setAll(value: boolean): void {
    this.matrix = new Array<boolean[]>(this.rowCount);
    for (let i = 0; i < this.matrix.length; i++) {
      this.matrix[i] = new Array<boolean>(this.columnCount).fill(value);
    }
  }

  set(i: number, j: number, value: boolean): void {
    this.matrix[i][j] = value;
  }

  get(i: number, j: number): boolean {
    return this.matrix[i][j];
  }

  getRowAlignedIndices(i: number): number[] {
    const indices: number[] = [];
    for (let j = 0; j < this.columnCount; j++) {
      if (this.matrix[i][j]) {
        indices.push(j);
      }
    }
    return indices;
  }

  getColumnAlignedIndices(j: number): number[] {
    const indices: number[] = [];
    for (let i = 0; i < this.rowCount; i++) {
      if (this.matrix[i][j]) {
        indices.push(i);
      }
    }
    return indices;
  }
}
