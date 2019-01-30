import { LatinSentenceTokenizer } from './latin-sentence-tokenizer';

describe('LatinSentenceTokenizer', () => {
  it('empty string', () => {
    const tokenizer = new LatinSentenceTokenizer();
    expect(tokenizer.tokenizeToStrings('')).toEqual([]);
  });

  it('single line', () => {
    const tokenizer = new LatinSentenceTokenizer();
    expect(tokenizer.tokenizeToStrings('This is a test.')).toEqual(['This is a test.']);
  });

  it('multiple lines', () => {
    const tokenizer = new LatinSentenceTokenizer();
    expect(tokenizer.tokenizeToStrings('This is the first sentence.\nThis is the second sentence.')).toEqual([
      'This is the first sentence.',
      'This is the second sentence.'
    ]);
  });

  it('two sentences', () => {
    const tokenizer = new LatinSentenceTokenizer();
    expect(tokenizer.tokenizeToStrings('This is the first sentence. This is the second sentence.')).toEqual([
      'This is the first sentence.',
      'This is the second sentence.'
    ]);
  });

  it('sentence with quotes', () => {
    const tokenizer = new LatinSentenceTokenizer();
    expect(tokenizer.tokenizeToStrings('"This is the first sentence." This is the second sentence.')).toEqual([
      '"This is the first sentence."',
      'This is the second sentence.'
    ]);
  });

  it('sentence with an internal quotation', () => {
    const tokenizer = new LatinSentenceTokenizer();
    expect(tokenizer.tokenizeToStrings('"This is the first sentence!" he said. This is the second sentence.')).toEqual([
      '"This is the first sentence!" he said.',
      'This is the second sentence.'
    ]);
  });

  it('sentence with parentheses', () => {
    const tokenizer = new LatinSentenceTokenizer();
    expect(tokenizer.tokenizeToStrings('This is the first sentence. (This is the second sentence.)')).toEqual([
      'This is the first sentence.',
      '(This is the second sentence.)'
    ]);
  });

  it('sentence with abbreviations', () => {
    const tokenizer = new LatinSentenceTokenizer(['mr', 'dr', 'ms']);
    expect(tokenizer.tokenizeToStrings('Mr. Smith went to Washington. This is the second sentence.')).toEqual([
      'Mr. Smith went to Washington.',
      'This is the second sentence.'
    ]);
  });

  it('incomplete sentence', () => {
    const tokenizer = new LatinSentenceTokenizer();
    expect(tokenizer.tokenizeToStrings('This is an incomplete sentence ')).toEqual(['This is an incomplete sentence ']);
  });

  it('complete sentence with a space at the end', () => {
    const tokenizer = new LatinSentenceTokenizer();
    expect(tokenizer.tokenizeToStrings('"This is a complete sentence." \n')).toEqual([
      '"This is a complete sentence."'
    ]);
  });
});
