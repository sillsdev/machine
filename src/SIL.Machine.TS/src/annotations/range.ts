export interface Range<TOffset = number> {
  readonly start: TOffset;
  readonly end: TOffset;
  readonly length: number;
}

class NumberRange implements Range<number> {
  constructor(public readonly start: number, public readonly end: number) {}

  get length(): number {
    return this.end - this.start;
  }
}

export function createRange<TOffset = number>(start: TOffset, end?: TOffset): Range<TOffset> {
  if (typeof start === 'number') {
    let endNum = start + 1;
    if (typeof end === 'number') {
      endNum = end;
    }
    return (new NumberRange(start, endNum) as unknown) as Range<TOffset>;
  }

  throw Error('Range type not supported.');
}
