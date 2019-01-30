import XRegExp from 'xregexp';

import { createRange, Range } from '../annotations/range';
import { StringTokenizer } from './string-tokenizer';

const WHITESPACE_REGEX: RegExp = XRegExp('^\\p{Z}$');

export class WhitespaceTokenizer extends StringTokenizer {
  tokenize(data: string, range: Range = createRange(0, data.length)): Range[] {
    const tokens: Range[] = [];
    let startIndex = -1;
    for (let i = range.start; i < range.end; i++) {
      if (WHITESPACE_REGEX.test(data[i])) {
        if (startIndex !== -1) {
          tokens.push(createRange(startIndex, i));
        }
        startIndex = -1;
      } else if (startIndex === -1) {
        startIndex = i;
      }
    }

    if (startIndex !== -1) {
      tokens.push(createRange(startIndex, range.end));
    }

    return tokens;
  }
}
