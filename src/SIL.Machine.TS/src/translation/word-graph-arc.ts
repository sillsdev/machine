import { Range } from '../annotations/range';
import { WordAlignmentMatrix } from './word-alignment-matrix';

export class WordGraphArc {
  constructor(
    public readonly prevState: number,
    public readonly nextState: number,
    public readonly score: number,
    public readonly words: string[],
    public readonly alignment: WordAlignmentMatrix,
    public readonly sourceSegmentRange: Range,
    public readonly isUnknown: boolean,
    public readonly wordConfidences: number[] = new Array<number>(words.length).fill(-1)
  ) {}
}
