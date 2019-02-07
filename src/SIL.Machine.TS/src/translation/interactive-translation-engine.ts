import { InteractiveTranslationSession } from './interactive-translation-session';
import { TranslationEngine } from './translation-engine';

export interface InteractiveTranslationEngine extends TranslationEngine {
  translateInteractively(n: number, segment: string[]): Promise<InteractiveTranslationSession>;
}
