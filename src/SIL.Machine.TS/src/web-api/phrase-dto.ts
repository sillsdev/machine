import { RangeDto } from './range-dto';

export interface PhraseDto {
  sourceSegmentRange: RangeDto;
  targetSegmentCut: number;
  confidence: number;
}
