import XRegExp from 'xregexp';

import { createRange, Range } from '../annotations/range';
import { WhitespaceTokenizer } from './whitespace-tokenizer';

const PUNCT_REGEX: RegExp = XRegExp('^\\p{P}|\\p{S}|\\p{Cc}$');
const INNER_WORD_PUNCT_REGEX: RegExp = XRegExp("^[&'\\-.:=?@\xAD\xB7\u2010\u2011\u2019\u2027]|_+");

export class LatinWordTokenizer extends WhitespaceTokenizer {
  private readonly abbreviations: Set<string>;

  constructor(abbreviations: string[] = []) {
    super();
    this.abbreviations = new Set<string>(abbreviations.map(a => a.toLowerCase()));
  }

  tokenize(data: string, range: Range = createRange(0, data.length)): Range[] {
    const tokens: Range[] = [];
    for (const charRange of super.tokenize(data, range)) {
      let wordStart = -1;
      let innerWordPunct = -1;
      let i = charRange.start;
      while (i < charRange.end) {
        if (PUNCT_REGEX.test(data[i])) {
          if (wordStart === -1) {
            tokens.push(createRange(i));
          } else if (innerWordPunct !== -1) {
            tokens.push(createRange(wordStart, innerWordPunct));
            tokens.push(createRange(innerWordPunct, i));
            wordStart = i;
          } else {
            const match = this.nextInnerWordPunct(data, i);
            if (match !== '') {
              innerWordPunct = i;
              i += match.length;
              continue;
            }

            tokens.push(createRange(wordStart, i));
            tokens.push(createRange(i));
            wordStart = -1;
          }
        } else if (wordStart === -1) {
          wordStart = i;
        }

        innerWordPunct = -1;
        i++;
      }

      if (wordStart !== -1) {
        if (innerWordPunct !== -1) {
          if (
            data.substring(innerWordPunct, charRange.end) === '.' &&
            this.abbreviations.has(data.substring(wordStart, innerWordPunct).toLowerCase())
          ) {
            tokens.push(createRange(wordStart, charRange.end));
          } else {
            tokens.push(createRange(wordStart, innerWordPunct));
            tokens.push(createRange(innerWordPunct, charRange.end));
          }
        } else {
          tokens.push(createRange(wordStart, charRange.end));
        }
      }
    }

    return tokens;
  }

  private nextInnerWordPunct(char: string, index: number): string {
    const result = INNER_WORD_PUNCT_REGEX.exec(char.substring(index));
    if (result == null) {
      return '';
    }

    return result[0];
  }
}
