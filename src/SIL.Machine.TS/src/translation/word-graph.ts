import { LOG_ZERO, logMultiply } from '../statistics/log-space';
import { WordGraphArc } from './word-graph-arc';

export const INITIAL_STATE: number = 0;

export class WordGraph {
  readonly finalStates: Set<number>;
  readonly stateCount: number;

  private readonly states: Map<number, StateInfo> = new Map<number, StateInfo>();

  constructor(
    public readonly arcs: WordGraphArc[] = [],
    finalStates: number[] = [],
    public readonly initialStateScore: number = 0
  ) {
    let maxState = -1;
    for (let i = 0; i < this.arcs.length; i++) {
      const arc = this.arcs[i];
      if (arc.nextState > maxState) {
        maxState = arc.nextState;
      }
      if (arc.prevState > maxState) {
        maxState = arc.prevState;
      }
      this.getOrCreateStateInfo(arc.prevState).nextArcIndices.push(i);
      this.getOrCreateStateInfo(arc.nextState).prevArcIndices.push(i);
    }
    this.stateCount = maxState + 1;
    this.finalStates = new Set<number>(finalStates);
  }

  get isEmpty(): boolean {
    return this.arcs.length === 0;
  }

  computeRestScores(): number[] {
    const restScores = new Array<number>(this.stateCount).fill(LOG_ZERO);
    for (const state of this.finalStates) {
      restScores[state] = this.initialStateScore;
    }

    for (let i = this.arcs.length - 1; i >= 0; i--) {
      const arc = this.arcs[i];
      const score = logMultiply(arc.score, restScores[arc.nextState]);
      if (score > restScores[arc.prevState]) {
        restScores[arc.prevState] = score;
      }
    }

    return restScores;
  }

  getBestPathFromStateToFinalState(state: number): WordGraphArc[] {
    const { prevScores, stateBestPrevArcs } = this.computePrevScores(state);

    let bestFinalStateScore = LOG_ZERO;
    let bestFinalState = 0;
    for (const finalState of this.finalStates) {
      const score = prevScores[finalState];
      if (bestFinalStateScore < score) {
        bestFinalState = finalState;
        bestFinalStateScore = score;
      }
    }

    if (!this.finalStates.has(bestFinalState)) {
      return [];
    }

    const arcs: WordGraphArc[] = [];
    let curState = bestFinalState;
    let end = false;
    while (!end) {
      if (curState === state) {
        end = true;
      } else {
        const arcIndex = stateBestPrevArcs[curState];
        const arc = this.arcs[arcIndex];
        arcs.push(arc);
        curState = arc.prevState;
      }
    }
    return arcs.reverse();
  }

  getNextArcIndices(state: number): number[] {
    const stateInfo = this.states.get(state);
    if (stateInfo != null) {
      return stateInfo.nextArcIndices;
    }
    return [];
  }

  private getOrCreateStateInfo(state: number): StateInfo {
    let stateInfo = this.states.get(state);
    if (stateInfo == null) {
      stateInfo = {
        prevArcIndices: [],
        nextArcIndices: []
      };
      this.states.set(state, stateInfo);
    }
    return stateInfo;
  }

  private computePrevScores(state: number): { prevScores: number[]; stateBestPrevArcs: number[] } {
    if (this.isEmpty) {
      return { prevScores: [], stateBestPrevArcs: [] };
    }

    const prevScores = new Array<number>(this.stateCount).fill(LOG_ZERO);
    const stateBestPrevArcs = new Array<number>(this.stateCount);

    if (state === INITIAL_STATE) {
      prevScores[INITIAL_STATE] = this.initialStateScore;
    } else {
      prevScores[state] = 0;
    }

    const accessibleStates = new Set<number>([state]);
    for (let arcIndex = 0; arcIndex < this.arcs.length; arcIndex++) {
      const arc = this.arcs[arcIndex];
      if (accessibleStates.has(arc.prevState)) {
        const score = logMultiply(arc.score, prevScores[arc.prevState]);
        if (score > prevScores[arc.nextState]) {
          prevScores[arc.nextState] = score;
          stateBestPrevArcs[arc.nextState] = arcIndex;
        }
        accessibleStates.add(arc.nextState);
      } else {
        if (!accessibleStates.has(arc.nextState)) {
          prevScores[arc.nextState] = LOG_ZERO;
        }
      }
    }

    return { prevScores, stateBestPrevArcs };
  }
}

interface StateInfo {
  readonly prevArcIndices: number[];
  readonly nextArcIndices: number[];
}
