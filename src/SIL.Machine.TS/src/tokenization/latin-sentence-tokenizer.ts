import XRegExp from 'xregexp';

import { createRange, Range } from '../annotations/range';
import { LatinWordTokenizer } from './latin-word-tokenizer';
import { LineSegmentTokenizer } from './line-segment-tokenizer';

const SENTENCE_TERMINALS: Set<string> = new Set<string>([
  '.',
  '!',
  '?',
  '\u203C',
  '\u203D',
  '\u2047',
  '\u2048',
  '\u2049',
  '\u3002',
  '\uFE52',
  '\uFE57',
  '\uFF01',
  '\uFF0E',
  '\uFF1F',
  '\uFF61'
]);
const CLOSING_QUOTES: Set<string> = new Set<string>(["'", '\u2019', '"', '\u201D', '»', '›']);
const CLOSING_BRACKETS: Set<string> = new Set<string>([']', ')']);
const LINE_TOKENIZER: LineSegmentTokenizer = new LineSegmentTokenizer();
const LOWER_REGEX: RegExp = XRegExp('^\\p{Ll}$');

export class LatinSentenceTokenizer extends LatinWordTokenizer {
  constructor(abbreviations: string[] = []) {
    super(abbreviations);
  }

  tokenize(data: string, range: Range = createRange(0, data.length)): Range[] {
    const tokens: Range[] = [];
    for (const lineRange of LINE_TOKENIZER.tokenize(data, range)) {
      let sentenceStart = -1;
      let sentenceEnd = -1;
      let inEnd = false;
      let hasEndQuotesBracket = false;
      for (const wordRange of super.tokenize(data, lineRange)) {
        if (sentenceStart === -1) {
          sentenceStart = wordRange.start;
        }
        const word = data.substring(wordRange.start, wordRange.end);
        if (!inEnd) {
          if (SENTENCE_TERMINALS.has(word)) {
            inEnd = true;
          }
        } else {
          if (CLOSING_QUOTES.has(word) || CLOSING_BRACKETS.has(word)) {
            hasEndQuotesBracket = true;
          } else if (hasEndQuotesBracket && LOWER_REGEX.test(word[0])) {
            inEnd = false;
            hasEndQuotesBracket = false;
          } else {
            tokens.push(createRange(sentenceStart, sentenceEnd));
            sentenceStart = wordRange.start;
            inEnd = false;
            hasEndQuotesBracket = false;
          }
        }
        sentenceEnd = wordRange.end;
      }

      if (sentenceStart !== -1 && sentenceEnd !== -1) {
        tokens.push(createRange(sentenceStart, inEnd ? sentenceEnd : lineRange.end));
      }
    }

    return tokens;
  }
}
