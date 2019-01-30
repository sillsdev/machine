import { TranslationSources } from '../translation/translation-sources';
import { AlignedWordPairDto } from './aligned-word-pair-dto';
import { PhraseDto } from './phrase-dto';

export interface TranslationResultDto {
  target: string[];
  confidences: number[];
  sources: TranslationSources[];
  alignment: AlignedWordPairDto[];
  phrases: PhraseDto[];
}
