import { TranslationResultDto } from './translation-result-dto';
import { WordGraphDto } from './word-graph-dto';

export interface InteractiveTranslationResultDto {
  wordGraph: WordGraphDto;
  ruleResult?: TranslationResultDto;
}
