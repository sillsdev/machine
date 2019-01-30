import { Range } from '../annotations/range';

export class Phrase {
  constructor(
    public readonly sourceSegmentRange: Range,
    public readonly targetSegmentCut: number,
    public readonly confidence: number
  ) {}
}
