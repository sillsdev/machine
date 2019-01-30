import { createRange, Range } from '../annotations/range';
import { StringTokenizer } from './string-tokenizer';

export class LineSegmentTokenizer extends StringTokenizer {
  tokenize(data: string, range: Range = createRange(0, data.length)): Range[] {
    const tokens: Range[] = [];
    let lineStart = range.start;
    for (let i = range.start; i < range.end; i++) {
      if (data[i] === '\n' || data[i] === '\r') {
        tokens.push(createRange(lineStart, i));
        if (data[i] === '\r' && i + 1 < range.end && data[i + 1] === '\n') {
          i++;
        }
        lineStart = i + 1;
      }
    }

    if (lineStart < range.end) {
      tokens.push(createRange(lineStart, range.end));
    }

    return tokens;
  }
}
