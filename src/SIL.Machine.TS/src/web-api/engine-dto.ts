import { ResourceDto } from './resource-dto';

export interface EngineDto extends ResourceDto {
  sourceLanguageTag: string;
  targetLanguageTag: string;
  isShared: boolean;
  projects: ResourceDto[];
  confidence: number;
  trainedSegmentCount: number;
}
