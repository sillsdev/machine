import { Range } from '../annotations/range';
import { WordAlignmentMatrix } from './word-alignment-matrix';

export class PhraseInfo {
  constructor(
    public readonly sourceSegmentRange: Range,
    public targetCut: number,
    public alignment: WordAlignmentMatrix
  ) {}
}
