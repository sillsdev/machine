import { ResourceDto } from './resource-dto';

export interface ProjectDto extends ResourceDto {
  sourceSegmentType: string;
  targetSegmentType: string;
  isShared: boolean;
  sourceLanguageTag: string;
  targetLanguageTag: string;
  engine: ResourceDto;
}
