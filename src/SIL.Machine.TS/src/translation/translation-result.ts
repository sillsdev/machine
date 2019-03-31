import { Phrase } from './phrase';
import { TranslationSources } from './translation-sources';
import { WordAlignmentMatrix } from './word-alignment-matrix';

export class TranslationResult {
  constructor(
    public readonly sourceSegment: string[],
    public readonly targetSegment: string[],
    public readonly wordConfidences: number[],
    public readonly wordSources: TranslationSources[],
    public readonly alignment: WordAlignmentMatrix,
    public readonly phrases: Phrase[]
  ) {
    if (this.wordConfidences.length !== this.targetSegment.length) {
      throw new Error('The confidences must be the same length as the target segment.');
    }
    if (this.wordSources.length !== this.targetSegment.length) {
      throw new Error('The sources must be the same length as the target segment.');
    }
    if (this.alignment.rowCount !== this.sourceSegment.length) {
      throw new Error('The alignment source length must be the same length as the source segment.');
    }
    if (this.alignment.columnCount !== this.targetSegment.length) {
      throw new Error('The alignment target length must be the same length as the target segment.');
    }
  }

  merge(prefixCount: number, threshold: number, otherResult: TranslationResult): TranslationResult {
    const mergedTargetSegment: string[] = [];
    const mergedConfidences: number[] = [];
    const mergedSources: number[] = [];
    const mergedAlignment: [number, number][] = [];
    for (let j = 0; j < this.targetSegment.length; j++) {
      const sourceIndices = this.alignment.getColumnAlignedIndices(j);
      if (sourceIndices.length === 0) {
        // target word doesn't align with anything
        mergedTargetSegment.push(this.targetSegment[j]);
        mergedConfidences.push(this.wordConfidences[j]);
        mergedSources.push(this.wordSources[j]);
      } else {
        // target word aligns with some source words
        if (j < prefixCount || this.wordConfidences[j] >= threshold) {
          // use target word of this result
          mergedTargetSegment.push(this.targetSegment[j]);
          mergedConfidences.push(this.wordConfidences[j]);
          let sources = this.wordSources[j];
          for (const i of sourceIndices) {
            // combine sources for any words that both this result
            // and the other result translated the same
            for (const jOther of otherResult.alignment.getRowAlignedIndices(i)) {
              const otherSources = otherResult.wordSources[jOther];
              if (
                otherSources !== TranslationSources.None &&
                otherResult.targetSegment[jOther] === this.targetSegment[j]
              ) {
                sources |= otherSources;
              }
            }

            mergedAlignment.push([i, mergedTargetSegment.length - 1]);
          }
          mergedSources.push(sources);
        } else {
          // use target words of other result
          let found = false;
          for (const i of sourceIndices) {
            for (const jOther of otherResult.alignment.getRowAlignedIndices(i)) {
              // look for any translated words from other result
              const otherSources = otherResult.wordSources[jOther];
              if (otherSources !== TranslationSources.None) {
                mergedTargetSegment.push(otherResult.targetSegment[jOther]);
                mergedConfidences.push(otherResult.wordConfidences[jOther]);
                mergedSources.push(otherSources);
                mergedAlignment.push([i, mergedTargetSegment.length - 1]);
                found = true;
              }
            }
          }

          if (!found) {
            // the other result had no translated words, so just use this result's target word
            mergedTargetSegment.push(this.targetSegment[j]);
            mergedConfidences.push(this.wordConfidences[j]);
            mergedSources.push(this.wordSources[j]);
            for (const i of sourceIndices) {
              mergedAlignment.push([i, mergedTargetSegment.length - 1]);
            }
          }
        }
      }
    }

    const alignment = new WordAlignmentMatrix(this.sourceSegment.length, mergedTargetSegment.length);
    for (const [i, j] of mergedAlignment) {
      alignment.set(i, j, true);
    }
    return new TranslationResult(
      this.sourceSegment,
      mergedTargetSegment,
      mergedConfidences,
      mergedSources,
      alignment,
      this.phrases
    );
  }

  getAlignedSourceSegment(prefixCount: number): string[] {
    let sourceLength = 0;
    for (const phrase of this.phrases) {
      if (phrase.targetSegmentCut > prefixCount) {
        break;
      }

      if (phrase.sourceSegmentRange.end > sourceLength) {
        sourceLength = phrase.sourceSegmentRange.end;
      }
    }

    return sourceLength === this.sourceSegment.length ? this.sourceSegment : this.sourceSegment.slice(0, sourceLength);
  }
}
