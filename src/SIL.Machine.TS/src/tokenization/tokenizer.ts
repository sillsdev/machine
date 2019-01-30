import { Range } from '../annotations/range';

export interface Tokenizer<TData = string, TOffset = number> {
  tokenize(data: TData, range?: Range<TOffset>): Range<TOffset>[];
  tokenizeToStrings(data: TData, range?: Range<TOffset>): string[];
}
