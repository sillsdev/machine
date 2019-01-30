import { LatinWordTokenizer } from './latin-word-tokenizer';

describe('LatinWordTokenizer', () => {
  it('empty string', () => {
    const tokenizer = new LatinWordTokenizer();
    expect(tokenizer.tokenizeToStrings('')).toEqual([]);
  });

  it('whitespace-only string', () => {
    const tokenizer = new LatinWordTokenizer();
    expect(tokenizer.tokenizeToStrings(' ')).toEqual([]);
  });

  it('word with punctuation at the end', () => {
    const tokenizer = new LatinWordTokenizer();
    expect(tokenizer.tokenizeToStrings('This is a test.')).toEqual(['This', 'is', 'a', 'test', '.']);
  });

  it('word with punctuation at the beginning', () => {
    const tokenizer = new LatinWordTokenizer();
    expect(tokenizer.tokenizeToStrings('Is this a "test"?')).toEqual(['Is', 'this', 'a', '"', 'test', '"', '?']);
  });

  it('word with internal punctuation', () => {
    const tokenizer = new LatinWordTokenizer();
    expect(tokenizer.tokenizeToStrings("This isn't a test.")).toEqual(['This', "isn't", 'a', 'test', '.']);
  });

  it('string with abbreviations', () => {
    const tokenizer = new LatinWordTokenizer(['mr', 'dr', 'ms']);
    expect(tokenizer.tokenizeToStrings('Mr. Smith went to Washington.')).toEqual([
      'Mr.',
      'Smith',
      'went',
      'to',
      'Washington',
      '.'
    ]);
  });

  it('string with a non-ASCII character', () => {
    const tokenizer = new LatinWordTokenizer();
    expect(tokenizer.tokenizeToStrings('This is—a test.')).toEqual(['This', 'is', '—', 'a', 'test', '.']);
  });
});
