import { WordGraphArcDto } from './word-graph-arc-dto';

export interface WordGraphDto {
  initialStateScore: number;
  finalStates: number[];
  arcs: WordGraphArcDto[];
}
