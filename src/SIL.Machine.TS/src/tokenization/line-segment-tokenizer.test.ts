import { LineSegmentTokenizer } from './line-segment-tokenizer';

describe('LineSegmentTokenizer', () => {
  it('empty string', () => {
    const tokenizer = new LineSegmentTokenizer();
    expect(tokenizer.tokenizeToStrings('')).toEqual([]);
  });

  it('single line', () => {
    const tokenizer = new LineSegmentTokenizer();
    expect(tokenizer.tokenizeToStrings('This is a test.')).toEqual(['This is a test.']);
  });

  it('multiple lines', () => {
    const tokenizer = new LineSegmentTokenizer();
    expect(tokenizer.tokenizeToStrings('This is the first sentence.\nThis is the second sentence.')).toEqual([
      'This is the first sentence.',
      'This is the second sentence.'
    ]);
  });

  it('string that ends with a newline', () => {
    const tokenizer = new LineSegmentTokenizer();
    expect(tokenizer.tokenizeToStrings('This is a test.\n')).toEqual(['This is a test.']);
  });

  it('string that ends with a newline and a space', () => {
    const tokenizer = new LineSegmentTokenizer();
    expect(tokenizer.tokenizeToStrings('This is a test.\n ')).toEqual(['This is a test.', ' ']);
  });

  it('string that ends with text and a space', () => {
    const tokenizer = new LineSegmentTokenizer();
    expect(tokenizer.tokenizeToStrings('This is the first sentence.\nThis is a partial sentence ')).toEqual([
      'This is the first sentence.',
      'This is a partial sentence '
    ]);
  });

  it('string that contains an empty line', () => {
    const tokenizer = new LineSegmentTokenizer();
    expect(tokenizer.tokenizeToStrings('This is the first sentence.\n\nThis is the third sentence.')).toEqual([
      'This is the first sentence.',
      '',
      'This is the third sentence.'
    ]);
  });

  it('string where the a line ends with a space', () => {
    const tokenizer = new LineSegmentTokenizer();
    expect(tokenizer.tokenizeToStrings('This is the first sentence. \nThis is the second sentence.')).toEqual([
      'This is the first sentence. ',
      'This is the second sentence.'
    ]);
  });
});
