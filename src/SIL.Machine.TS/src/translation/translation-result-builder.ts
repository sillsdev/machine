import { Range } from '../annotations/range';
import { EditOperation } from './edit-operation';
import { Phrase } from './phrase';
import { PhraseInfo } from './phrase-info';
import { TranslationResult } from './translation-result';
import { TranslationSources } from './translation-sources';
import { WordAlignmentMatrix } from './word-alignment-matrix';

export class TranslationResultBuilder {
  public readonly words: string[] = [];
  public readonly confidences: number[] = [];
  public readonly phrases: PhraseInfo[] = [];

  private readonly unknownWords: Set<number> = new Set<number>();
  private readonly uncorrectedPrefixWords: Set<number> = new Set<number>();

  appendWord(word: string, confidence: number = -1, isUnknown: boolean = false): void {
    this.words.push(word);
    this.confidences.push(confidence);
    if (isUnknown) {
      this.unknownWords.add(this.words.length - 1);
    }
  }

  markPhrase(sourceSegmentRange: Range, alignment: WordAlignmentMatrix): void {
    this.phrases.push(new PhraseInfo(sourceSegmentRange, this.words.length, alignment));
  }

  setConfidence(index: number, confidence: number): void {
    this.confidences[index] = confidence;
  }

  correctPrefix(
    wordOps: EditOperation[],
    charOps: EditOperation[],
    prefix: string[],
    isLastWordComplete: boolean
  ): number {
    let alignmentColsToCopy: number[] = [];
    let i = 0;
    let j = 0;
    let k = 0;
    for (const wordOp of wordOps) {
      switch (wordOp) {
        case EditOperation.Insert:
          this.words.splice(j, 0, prefix[j]);
          this.confidences.splice(j, 0, -1);
          alignmentColsToCopy.push(-1);
          for (let l = k; l < this.phrases.length; l++) {
            this.phrases[l].targetCut++;
          }
          j++;
          break;

        case EditOperation.Delete:
          this.words.splice(j, 1);
          this.confidences.splice(j, 1);
          i++;
          if (k < this.phrases.length) {
            for (let l = k; l < this.phrases.length; l++) {
              this.phrases[l].targetCut--;
            }

            if (
              this.phrases[k].targetCut <= 0 ||
              (k > 0 && this.phrases[k].targetCut === this.phrases[k - 1].targetCut)
            ) {
              this.phrases.splice(k, 1);
              alignmentColsToCopy = [];
              i = 0;
            } else if (j >= this.phrases[k].targetCut) {
              this.resizeAlignment(k, alignmentColsToCopy);
              alignmentColsToCopy = [];
              i = 0;
              k++;
            }
          }
          break;

        case EditOperation.Hit:
        case EditOperation.Substitute:
          if (wordOp === EditOperation.Substitute || j < prefix.length - 1 || isLastWordComplete) {
            this.words[j] = prefix[j];
          } else {
            this.words[j] = this.correctWord(charOps, this.words[j], prefix[j]);
          }

          if (wordOp === EditOperation.Substitute) {
            this.confidences[j] = -1;
          } else if (wordOp === EditOperation.Hit) {
            this.uncorrectedPrefixWords.add(j);
          }

          alignmentColsToCopy.push(i);

          i++;
          j++;
          if (k < this.phrases.length && j >= this.phrases[k].targetCut) {
            this.resizeAlignment(k, alignmentColsToCopy);
            alignmentColsToCopy = [];
            i = 0;
            k++;
          }
          break;
      }
    }

    while (j < this.words.length) {
      alignmentColsToCopy.push(i);

      i++;
      j++;
      if (k < this.phrases.length && j >= this.phrases[k].targetCut) {
        this.resizeAlignment(k, alignmentColsToCopy);
        alignmentColsToCopy = [];
        break;
      }
    }

    return alignmentColsToCopy.length;
  }

  toResult(sourceSegment: string[], prefixCount: number = 0): TranslationResult {
    const confidences = this.confidences.slice();
    const sources = new Array<TranslationSources>(this.words.length);
    const alignment = new WordAlignmentMatrix(sourceSegment.length, this.words.length);
    const phrases: Phrase[] = [];
    let trgPhraseStartIndex = 0;
    for (const phraseInfo of this.phrases) {
      let confidence = Number.MAX_VALUE;
      for (let j = trgPhraseStartIndex; j < phraseInfo.targetCut; j++) {
        for (let i = phraseInfo.sourceSegmentRange.start; i < phraseInfo.sourceSegmentRange.end; i++) {
          const aligned = phraseInfo.alignment.get(i - phraseInfo.sourceSegmentRange.start, j - trgPhraseStartIndex);
          if (aligned) {
            alignment.set(i, j, true);
          }
        }

        if (j < prefixCount) {
          sources[j] = TranslationSources.Prefix;
          if (this.uncorrectedPrefixWords.has(j)) {
            sources[j] |= TranslationSources.Smt;
          }
        } else if (this.unknownWords.has(j)) {
          sources[j] = TranslationSources.None;
        } else {
          sources[j] = TranslationSources.Smt;
        }

        confidence = Math.min(confidence, this.confidences[j]);
      }

      phrases.push(new Phrase(phraseInfo.sourceSegmentRange, phraseInfo.targetCut, confidence));
      trgPhraseStartIndex = phraseInfo.targetCut;
    }

    return new TranslationResult(sourceSegment, this.words, confidences, sources, alignment, phrases);
  }

  private resizeAlignment(phraseIndex: number, colsToCopy: number[]): void {
    const curAlignment = this.phrases[phraseIndex].alignment;
    if (colsToCopy.length === curAlignment.columnCount) {
      return;
    }

    const newAlignment = new WordAlignmentMatrix(curAlignment.rowCount, colsToCopy.length);
    for (let j = 0; j < newAlignment.columnCount; j++) {
      if (colsToCopy[j] !== -1) {
        for (let i = 0; i < newAlignment.rowCount; i++) {
          newAlignment.set(i, j, curAlignment.get(i, colsToCopy[j]));
        }
      }
    }

    this.phrases[phraseIndex].alignment = newAlignment;
  }

  private correctWord(charOps: EditOperation[], word: string, prefix: string): string {
    let corrected = '';
    let i = 0;
    let j = 0;
    for (const charOp of charOps) {
      switch (charOp) {
        case EditOperation.Hit:
          corrected += word[i];
          i++;
          j++;
          break;

        case EditOperation.Insert:
          corrected += prefix[j];
          j++;
          break;

        case EditOperation.Delete:
          i++;
          break;

        case EditOperation.Substitute:
          corrected += prefix[j];
          i++;
          j++;
          break;
      }
    }

    corrected += word.substring(i);
    return corrected;
  }
}
