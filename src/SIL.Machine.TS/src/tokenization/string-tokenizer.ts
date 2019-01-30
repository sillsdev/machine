import { createRange, Range } from '../annotations/range';
import { Tokenizer } from './tokenizer';

export abstract class StringTokenizer implements Tokenizer {
  abstract tokenize(data: string, range?: Range): Range[];

  tokenizeToStrings(data: string, range: Range = createRange(0, data.length)): string[] {
    return this.tokenize(data, range).map(r => data.substring(r.start, r.end));
  }
}
