import { PriorityQueue } from 'typescript-collections';

import { LOG_ZERO } from '../statistics/log-space';
import { EcmScoreInfo } from './ecm-score-info';
import { ErrorCorrectionModel } from './error-correction-model';
import { TranslationResult } from './translation-result';
import { TranslationResultBuilder } from './translation-result-builder';
import { WordAlignmentMatrix } from './word-alignment-matrix';
import { INITIAL_STATE, WordGraph } from './word-graph';
import { WordGraphArc } from './word-graph-arc';

export class ErrorCorrectionWordGraphProcessor {
  confidenceThreshold: number = 0;

  private readonly restScores: number[];
  private readonly stateEcmScoreInfos: EcmScoreInfo[] = [];
  private readonly arcEcmScoreInfos: EcmScoreInfo[][] = [];
  private readonly stateBestScores: number[][] = [];
  private readonly stateWordGraphScores: number[] = [];
  private readonly stateBestPrevArcs: number[][] = [];
  private readonly statesInvolvedInArcs: Set<number> = new Set<number>();
  private prevPrefix: string[] = [];
  private prevIsLastWordComplete: boolean = false;

  constructor(
    private readonly ecm: ErrorCorrectionModel,
    private readonly sourceSegment: string[],
    private readonly wordGraph: WordGraph,
    public readonly ecmWeight = 1,
    public readonly wordGraphWeight = 1
  ) {
    this.restScores = this.wordGraph.computeRestScores();

    this.initStates();
    this.initArcs();
  }

  correct(prefix: string[], isLastWordComplete: boolean, n: number): TranslationResult[] {
    // get valid portion of the processed prefix vector
    let validProcPrefixCount = 0;
    for (let i = 0; i < this.prevPrefix.length; i++) {
      if (i >= prefix.length) {
        break;
      }

      if (i === this.prevPrefix.length - 1 && i === prefix.length - 1) {
        if (this.prevPrefix[i] === prefix[i] && this.prevIsLastWordComplete === isLastWordComplete) {
          validProcPrefixCount++;
        }
      } else if (this.prevPrefix[i] === prefix[i]) {
        validProcPrefixCount++;
      }
    }

    const diffSize = this.prevPrefix.length - validProcPrefixCount;
    if (diffSize > 0) {
      // adjust size of info for arcs
      for (const esis of this.arcEcmScoreInfos) {
        for (const esi of esis) {
          for (let i = 0; i < diffSize; i++) {
            esi.removeLast();
          }
        }
      }

      // adjust size of info for states
      for (const state of this.statesInvolvedInArcs) {
        for (let i = 0; i < diffSize; i++) {
          this.stateEcmScoreInfos[state].removeLast();
          this.stateBestScores[state].pop();
          this.stateBestPrevArcs[state].pop();
        }
      }
    }

    // get difference between prefix and valid portion of processed prefix
    const prefixDiff = new Array<string>(prefix.length - validProcPrefixCount);
    for (let i = 0; i < prefixDiff.length; i++) {
      prefixDiff[i] = prefix[validProcPrefixCount + i];
    }

    // process word-graph given prefix difference
    this.processWordGraphForPrefixDiff(prefixDiff, isLastWordComplete);

    this.prevPrefix = prefix;
    this.prevIsLastWordComplete = isLastWordComplete;

    const queue = new PriorityQueue<Hypothesis>((x, y) => {
      if (x.score < y.score) {
        return -1;
      } else if (x.score > y.score) {
        return 1;
      } else {
        return 0;
      }
    });
    this.getStateHypotheses(queue);
    this.getSubStateHypotheses(queue);

    const results: TranslationResult[] = [];
    for (const hypothesis of this.nbestSearch(n, queue)) {
      const builder = new TranslationResultBuilder();
      this.buildCorrectionFromHypothesis(builder, prefix, isLastWordComplete, hypothesis);
      results.push(builder.toResult(this.sourceSegment, prefix.length));
    }
    return results;
  }

  private initStates(): void {
    for (let i = 0; i < this.wordGraph.stateCount; i++) {
      this.stateEcmScoreInfos.push(new EcmScoreInfo());
      this.stateWordGraphScores.push(LOG_ZERO);
      this.stateBestScores.push([]);
      this.stateBestPrevArcs.push([]);
    }

    if (!this.wordGraph.isEmpty) {
      this.ecm.setupInitialEsi(this.stateEcmScoreInfos[INITIAL_STATE]);
      this.updateInitialStateBestScores();
    }
  }

  private initArcs(): void {
    for (let arcIndex = 0; arcIndex < this.wordGraph.arcs.length; arcIndex++) {
      const arc = this.wordGraph.arcs[arcIndex];

      // init ecm score info for each word of arc
      let prevEsi = this.stateEcmScoreInfos[arc.prevState];
      const esis: EcmScoreInfo[] = [];
      for (const word of arc.words) {
        const esi = new EcmScoreInfo();
        this.ecm.setupEsi(esi, prevEsi, word);
        esis.push(esi);
        prevEsi = esi;
      }
      this.arcEcmScoreInfos.push(esis);

      // init best scores for the arc's successive state
      this.updateStateBestScores(arcIndex, 0);

      this.statesInvolvedInArcs.add(arc.prevState);
      this.statesInvolvedInArcs.add(arc.nextState);
    }
  }

  private updateInitialStateBestScores(): void {
    const esi = this.stateEcmScoreInfos[INITIAL_STATE];

    this.stateWordGraphScores[INITIAL_STATE] = this.wordGraph.initialStateScore;

    const bestScores = this.stateBestScores[INITIAL_STATE];
    const bestPrevArcs = this.stateBestPrevArcs[INITIAL_STATE];

    bestScores.length = 0;
    bestPrevArcs.length = 0;

    for (const score of esi.scores) {
      bestScores.push(this.ecmWeight * -score + this.wordGraphWeight * this.wordGraph.initialStateScore);
      bestPrevArcs.push(Number.MAX_SAFE_INTEGER);
    }
  }

  private updateStateBestScores(arcIndex: number, prefixDiffSize: number): void {
    const arc = this.wordGraph.arcs[arcIndex];
    const arcEsis = this.arcEcmScoreInfos[arcIndex];

    const prevEsi = arcEsis.length === 0 ? this.stateEcmScoreInfos[arc.prevState] : arcEsis[arcEsis.length - 1];

    const wordGraphScore = this.stateWordGraphScores[arc.prevState] + arc.score;

    const nextStateBestScores = this.stateBestScores[arc.nextState];
    const nextStateBestPrevArcs = this.stateBestPrevArcs[arc.nextState];

    const positions: number[] = [];
    const startPos = prefixDiffSize === 0 ? 0 : prevEsi.scores.length - prefixDiffSize;
    for (let i = startPos; i < prevEsi.scores.length; i++) {
      const newScore = this.ecmWeight * -prevEsi.scores[i] + this.wordGraphWeight * wordGraphScore;

      if (i === nextStateBestScores.length || nextStateBestScores[i] < newScore) {
        this.addOrReplace(nextStateBestScores, i, newScore);
        positions.push(i);
        this.addOrReplace(nextStateBestPrevArcs, i, arcIndex);
      }
    }

    this.stateEcmScoreInfos[arc.nextState].updatePositions(prevEsi, positions);

    if (wordGraphScore > this.stateWordGraphScores[arc.nextState]) {
      this.stateWordGraphScores[arc.nextState] = wordGraphScore;
    }
  }

  private addOrReplace<T>(list: T[], index: number, item: T): void {
    if (index > list.length) {
      throw new Error('Index is out of range.');
    }

    if (index === list.length) {
      list.push(item);
    } else {
      list[index] = item;
    }
  }

  private processWordGraphForPrefixDiff(prefixDiff: string[], isLastWordComplete: boolean): void {
    if (prefixDiff.length === 0) {
      return;
    }

    if (!this.wordGraph.isEmpty) {
      const prevInitialEsi = this.stateEcmScoreInfos[INITIAL_STATE];
      this.ecm.extendInitialEsi(this.stateEcmScoreInfos[INITIAL_STATE], prevInitialEsi, prefixDiff);
      this.updateInitialStateBestScores();
    }

    for (let arcIndex = 0; arcIndex < this.wordGraph.arcs.length; arcIndex++) {
      const arc = this.wordGraph.arcs[arcIndex];

      // update ecm score info for each word of arc
      let prevEsi = this.stateEcmScoreInfos[arc.prevState];
      const esis = this.arcEcmScoreInfos[arcIndex];
      while (esis.length < arc.words.length) {
        esis.push(new EcmScoreInfo());
      }
      for (let i = 0; i < arc.words.length; i++) {
        const esi = esis[i];
        this.ecm.extendEsi(esi, prevEsi, arc.isUnknown ? '' : arc.words[i], prefixDiff, isLastWordComplete);
        prevEsi = esi;
      }

      // update best scores for the arc's successive state
      this.updateStateBestScores(arcIndex, prefixDiff.length);
    }
  }

  private getStateHypotheses(queue: PriorityQueue<Hypothesis>): void {
    for (const state of this.statesInvolvedInArcs) {
      const restScore = this.restScores[state];
      const bestScores = this.stateBestScores[state];

      const score = bestScores[bestScores.length - 1] + this.wordGraphWeight * restScore;
      queue.enqueue(new Hypothesis(score, state));
    }
  }

  private getSubStateHypotheses(queue: PriorityQueue<Hypothesis>): void {
    for (let arcIndex = 0; arcIndex < this.wordGraph.arcs.length; arcIndex++) {
      const arc = this.wordGraph.arcs[arcIndex];
      if (arc.words.length > 1 && !this.isArcPruned(arc)) {
        const wordGraphScore = this.stateWordGraphScores[arc.prevState] + arc.score;

        for (let i = 0; i < arc.words.length; i++) {
          const esi = this.arcEcmScoreInfos[arcIndex][i];
          const score =
            this.wordGraphWeight * wordGraphScore +
            this.ecmWeight * -esi.scores[esi.scores.length - 1] +
            this.wordGraphWeight * this.restScores[arc.nextState];
          queue.enqueue(new Hypothesis(score, arc.nextState, arcIndex, i));
        }
      }
    }
  }

  private isArcPruned(arc: WordGraphArc): boolean {
    return !arc.isUnknown && arc.wordConfidences.some(c => c < this.confidenceThreshold);
  }

  private nbestSearch(n: number, queue: PriorityQueue<Hypothesis>): Hypothesis[] {
    const nbest: Hypothesis[] = [];
    while (!queue.isEmpty()) {
      const hypothesis = queue.dequeue();
      if (hypothesis == null) {
        break;
      }
      const lastState =
        hypothesis.arcs.length === 0 ? hypothesis.startState : hypothesis.arcs[hypothesis.arcs.length - 1].nextState;

      if (this.wordGraph.finalStates.has(lastState)) {
        nbest.push(hypothesis);
        if (nbest.length === n) {
          break;
        }
      } else if (this.confidenceThreshold <= 0) {
        hypothesis.arcs.push(...this.wordGraph.getBestPathFromStateToFinalState(lastState));
        nbest.push(hypothesis);
        if (nbest.length === n) {
          break;
        }
      } else {
        const score = hypothesis.score - this.wordGraphWeight * this.restScores[lastState];
        const arcIndices = this.wordGraph.getNextArcIndices(lastState);
        let enqueuedArc = false;
        for (let i = 0; i < arcIndices.length; i++) {
          const arcIndex = arcIndices[i];
          const arc = this.wordGraph.arcs[arcIndex];
          if (this.isArcPruned(arc)) {
            continue;
          }

          let newHypothesis = hypothesis;
          if (i < arcIndices.length - 1) {
            newHypothesis = newHypothesis.clone();
          }
          newHypothesis.score = score;
          newHypothesis.score += arc.score;
          newHypothesis.score += this.restScores[arc.nextState];
          newHypothesis.arcs.push(arc);
          queue.enqueue(newHypothesis);
          enqueuedArc = true;
        }

        if (!enqueuedArc && (hypothesis.startArcIndex !== -1 || hypothesis.arcs.length > 0)) {
          hypothesis.arcs.push(...this.wordGraph.getBestPathFromStateToFinalState(lastState));
          nbest.push(hypothesis);
          if (nbest.length === n) {
            break;
          }
        }
      }
    }
    return nbest;
  }

  private buildCorrectionFromHypothesis(
    builder: TranslationResultBuilder,
    prefix: string[],
    isLastWordComplete: boolean,
    hypothesis: Hypothesis
  ): void {
    let uncorrectedPrefixLen: number;
    if (hypothesis.startArcIndex === -1) {
      this.addBestUncorrectedPrefixState(builder, prefix.length, hypothesis.startState);
      uncorrectedPrefixLen = builder.words.length;
    } else {
      this.addBestUncorrectedPrefixSubState(
        builder,
        prefix.length,
        hypothesis.startArcIndex,
        hypothesis.startArcWordIndex
      );
      const firstArc = this.wordGraph.arcs[hypothesis.startArcIndex];
      uncorrectedPrefixLen = builder.words.length - (firstArc.words.length - hypothesis.startArcWordIndex) + 1;
    }

    let alignmentColsToAddCount = this.ecm.correctPrefix(builder, uncorrectedPrefixLen, prefix, isLastWordComplete);

    for (const arc of hypothesis.arcs) {
      this.updateCorrectionFromArc(builder, arc, false, alignmentColsToAddCount);
      alignmentColsToAddCount = 0;
    }
  }

  private addBestUncorrectedPrefixState(builder: TranslationResultBuilder, procPrefixPos: number, state: number): void {
    const arcs: WordGraphArc[] = [];

    let curState = state;
    let curProcPrefixPos = procPrefixPos;
    while (curState !== 0) {
      const arcIndex = this.stateBestPrevArcs[curState][curProcPrefixPos];
      const arc = this.wordGraph.arcs[arcIndex];

      for (let i = arc.words.length - 1; i >= 0; i--) {
        const predPrefixWords = this.arcEcmScoreInfos[arcIndex][i].getLastInsPrefixWordFromEsi();
        curProcPrefixPos = predPrefixWords[curProcPrefixPos];
      }

      arcs.unshift(arc);

      curState = arc.prevState;
    }

    for (const arc of arcs) {
      this.updateCorrectionFromArc(builder, arc, true, 0);
    }
  }

  private addBestUncorrectedPrefixSubState(
    builder: TranslationResultBuilder,
    procPrefixPos: number,
    arcIndex: number,
    arcWordIndex: number
  ): void {
    const arc = this.wordGraph.arcs[arcIndex];

    let curProcPrefixPos = procPrefixPos;
    for (let i = arcWordIndex; i >= 0; i--) {
      const predPrefixWords = this.arcEcmScoreInfos[arcIndex][i].getLastInsPrefixWordFromEsi();
      curProcPrefixPos = predPrefixWords[curProcPrefixPos];
    }

    this.addBestUncorrectedPrefixState(builder, curProcPrefixPos, arc.prevState);

    this.updateCorrectionFromArc(builder, arc, true, 0);
  }

  private updateCorrectionFromArc(
    builder: TranslationResultBuilder,
    arc: WordGraphArc,
    isPrefix: boolean,
    alignmentColsToAddCount: number
  ): void {
    for (let i = 0; i < arc.words.length; i++) {
      builder.appendWord(arc.words[i], arc.wordConfidences[i], !isPrefix && arc.isUnknown);
    }

    let alignment = arc.alignment;
    if (alignmentColsToAddCount > 0) {
      const newAlignment = new WordAlignmentMatrix(alignment.rowCount, alignment.columnCount + alignmentColsToAddCount);
      for (let j = 0; j < alignment.columnCount; j++) {
        for (let i = 0; i < alignment.rowCount; i++) {
          newAlignment.set(i, alignmentColsToAddCount + j, alignment.get(i, j));
        }
      }
      alignment = newAlignment;
    }

    builder.markPhrase(arc.sourceSegmentRange, alignment);
  }
}

class Hypothesis {
  readonly arcs: WordGraphArc[] = [];

  constructor(
    public score: number,
    public readonly startState: number,
    public readonly startArcIndex: number = -1,
    public readonly startArcWordIndex = -1
  ) {}

  clone(): Hypothesis {
    const clone = new Hypothesis(this.score, this.startState, this.startArcIndex, this.startArcWordIndex);
    clone.arcs.push(...this.arcs);
    return clone;
  }
}
