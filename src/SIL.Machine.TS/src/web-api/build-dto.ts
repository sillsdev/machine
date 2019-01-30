import { ResourceDto } from './resource-dto';

export interface BuildDto extends ResourceDto {
  revision: number;
  engine: ResourceDto;
  percentCompleted: number;
  message: string;
  state: string;
}
