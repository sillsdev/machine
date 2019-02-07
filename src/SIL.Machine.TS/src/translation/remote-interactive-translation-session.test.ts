import { instance, mock } from 'ts-mockito';

import { createRange, Range } from '../annotations/range';
import { WebApiClient } from '../web-api/web-api-client';
import { MAX_SEGMENT_LENGTH } from './constants';
import { ErrorCorrectionModel } from './error-correction-model';
import { HybridInteractiveTranslationResult } from './hybrid-interactive-translation-result';
import { RemoteInteractiveTranslationSession } from './remote-interactive-translation-session';
import { TranslationResult } from './translation-result';
import { TranslationSources } from './translation-sources';
import { WordAlignmentMatrix } from './word-alignment-matrix';
import { WordGraph } from './word-graph';
import { WordGraphArc } from './word-graph-arc';

describe('RemoteInteractiveTranslationSession', () => {
  it('empty prefix', () => {
    const session = createSession();
    const result = session.currentResults[0];
    expect(result.targetSegment.join(' ')).toEqual('In the beginning the Word already existía .');
  });

  it('add one complete word to prefix', () => {
    const session = createSession();
    const result = session.appendToPrefix('In', true)[0];
    expect(result.targetSegment.join(' ')).toEqual('In the beginning the Word already existía .');
  });

  it('add one partial word to prefix', () => {
    const session = createSession();
    session.appendToPrefix('In', true);
    const result = session.appendToPrefix('t', false)[0];
    expect(result.targetSegment.join(' ')).toEqual('In the beginning the Word already existía .');
  });

  it('remove one word from prefix', () => {
    const session = createSession();
    session.appendWordsToPrefix(['In', 'the', 'beginning']);
    const result = session.setPrefix(['In', 'the'], true)[0];
    expect(result.targetSegment.join(' ')).toEqual('In the beginning the Word already existía .');
  });

  it('remove entire prefix', () => {
    const session = createSession();
    session.appendWordsToPrefix(['In', 'the', 'beginning']);
    const result = session.setPrefix([], true)[0];
    expect(result.targetSegment.join(' ')).toEqual('In the beginning the Word already existía .');
  });

  it('source segment valid', () => {
    const session = createSession();
    expect(session.isSourceSegmentValid).toBeTruthy();
  });

  it('source segment invalid', () => {
    const mockedRestClient = mock(WebApiClient);
    const sourceSegment = Array<string>(MAX_SEGMENT_LENGTH + 1);
    sourceSegment.fill('word', 0, -1);
    sourceSegment[sourceSegment.length - 1] = '.';
    const session = new RemoteInteractiveTranslationSession(
      instance(mockedRestClient),
      new ErrorCorrectionModel(),
      'project01',
      1,
      sourceSegment,
      new HybridInteractiveTranslationResult(new WordGraph())
    );
    expect(session.isSourceSegmentValid).toBeFalsy();
  });
});

function createSession(): RemoteInteractiveTranslationSession {
  const mockedRestClient = mock(WebApiClient);

  const sourceSegment = ['En', 'el', 'principio', 'la', 'Palabra', 'ya', 'existía', '.'];

  const wordGraph = new WordGraph(
    [
      createArc({
        prevState: 0,
        nextState: 1,
        score: -22.4162,
        words: ['now', 'It'],
        wordConfidences: [0.00006755903, 0.0116618536],
        sourceSegmentRange: createRange(0, 2),
        isUnknown: false,
        alignment: [{ i: 0, j: 1 }, { i: 1, j: 0 }]
      }),
      createArc({
        prevState: 0,
        nextState: 2,
        score: -23.5761,
        words: ['In', 'your'],
        wordConfidences: [0.355293363, 0.0000941652761],
        sourceSegmentRange: createRange(0, 2),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }, { i: 1, j: 1 }]
      }),
      createArc({
        prevState: 0,
        nextState: 3,
        score: -11.1167,
        words: ['In', 'the'],
        wordConfidences: [0.355293363, 0.5004668],
        sourceSegmentRange: createRange(0, 2),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }, { i: 1, j: 1 }]
      }),
      createArc({
        prevState: 0,
        nextState: 4,
        score: -13.7804,
        words: ['In'],
        wordConfidences: [0.355293363],
        sourceSegmentRange: createRange(0, 1),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 3,
        nextState: 5,
        score: -12.9695,
        words: ['beginning'],
        wordConfidences: [0.348795831],
        sourceSegmentRange: createRange(2, 3),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 4,
        nextState: 5,
        score: -7.68319,
        words: ['the', 'beginning'],
        wordConfidences: [0.5004668, 0.348795831],
        sourceSegmentRange: createRange(1, 3),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }, { i: 1, j: 1 }]
      }),
      createArc({
        prevState: 4,
        nextState: 3,
        score: -14.4373,
        words: ['the'],
        wordConfidences: [0.5004668],
        sourceSegmentRange: createRange(1, 2),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 5,
        nextState: 6,
        score: -19.3042,
        words: ['his', 'Word'],
        wordConfidences: [0.00347203249, 0.477621228],
        sourceSegmentRange: createRange(3, 5),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }, { i: 1, j: 1 }]
      }),
      createArc({
        prevState: 5,
        nextState: 7,
        score: -8.49148,
        words: ['the', 'Word'],
        wordConfidences: [0.346071422, 0.477621228],
        sourceSegmentRange: createRange(3, 5),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }, { i: 1, j: 1 }]
      }),
      createArc({
        prevState: 1,
        nextState: 8,
        score: -15.2926,
        words: ['beginning'],
        wordConfidences: [0.348795831],
        sourceSegmentRange: createRange(2, 3),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 2,
        nextState: 9,
        score: -15.2926,
        words: ['beginning'],
        wordConfidences: [0.348795831],
        sourceSegmentRange: createRange(2, 3),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 7,
        nextState: 10,
        score: -14.3453,
        words: ['already'],
        wordConfidences: [0.2259867],
        sourceSegmentRange: createRange(5, 6),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 8,
        nextState: 6,
        score: -19.3042,
        words: ['his', 'Word'],
        wordConfidences: [0.00347203249, 0.477621228],
        sourceSegmentRange: createRange(3, 5),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }, { i: 1, j: 1 }]
      }),
      createArc({
        prevState: 8,
        nextState: 7,
        score: -8.49148,
        words: ['the', 'Word'],
        wordConfidences: [0.346071422, 0.477621228],
        sourceSegmentRange: createRange(3, 5),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }, { i: 1, j: 1 }]
      }),
      createArc({
        prevState: 9,
        nextState: 6,
        score: -19.3042,
        words: ['his', 'Word'],
        wordConfidences: [0.00347203249, 0.477621228],
        sourceSegmentRange: createRange(3, 5),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }, { i: 1, j: 1 }]
      }),
      createArc({
        prevState: 9,
        nextState: 7,
        score: -8.49148,
        words: ['the', 'Word'],
        wordConfidences: [0.346071422, 0.477621228],
        sourceSegmentRange: createRange(3, 5),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }, { i: 1, j: 1 }]
      }),
      createArc({
        prevState: 6,
        nextState: 10,
        score: -14.0526,
        words: ['already'],
        wordConfidences: [0.2259867],
        sourceSegmentRange: createRange(5, 6),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 10,
        nextState: 11,
        score: 51.1117,
        words: ['existía'],
        wordConfidences: [0.0],
        sourceSegmentRange: createRange(6, 7),
        isUnknown: true,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 11,
        nextState: 12,
        score: -29.0049,
        words: ['you', '.'],
        wordConfidences: [0.005803475, 0.317073762],
        sourceSegmentRange: createRange(7, 8),
        isUnknown: false,
        alignment: [{ i: 0, j: 1 }]
      }),
      createArc({
        prevState: 11,
        nextState: 13,
        score: -27.7143,
        words: ['to'],
        wordConfidences: [0.038961038],
        sourceSegmentRange: createRange(7, 8),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 11,
        nextState: 14,
        score: -30.0868,
        words: ['.', '‘'],
        wordConfidences: [0.317073762, 0.06190489],
        sourceSegmentRange: createRange(7, 8),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 11,
        nextState: 15,
        score: -30.1586,
        words: ['.', 'he'],
        wordConfidences: [0.317073762, 0.06702433],
        sourceSegmentRange: createRange(7, 8),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 11,
        nextState: 16,
        score: -28.2444,
        words: ['.', 'the'],
        wordConfidences: [0.317073762, 0.115540564],
        sourceSegmentRange: createRange(7, 8),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 11,
        nextState: 17,
        score: -23.8056,
        words: ['and'],
        wordConfidences: [0.08047272],
        sourceSegmentRange: createRange(7, 8),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 11,
        nextState: 18,
        score: -23.5842,
        words: ['the'],
        wordConfidences: [0.09361572],
        sourceSegmentRange: createRange(7, 8),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 11,
        nextState: 19,
        score: -18.8988,
        words: [','],
        wordConfidences: [0.1428188],
        sourceSegmentRange: createRange(7, 8),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 11,
        nextState: 20,
        score: -11.9218,
        words: ['.', '’'],
        wordConfidences: [0.317073762, 0.018057242],
        sourceSegmentRange: createRange(7, 8),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      }),
      createArc({
        prevState: 11,
        nextState: 21,
        score: -3.51852,
        words: ['.'],
        wordConfidences: [0.317073762],
        sourceSegmentRange: createRange(7, 8),
        isUnknown: false,
        alignment: [{ i: 0, j: 0 }]
      })
    ],
    [12, 13, 14, 15, 16, 17, 18, 19, 20, 21],
    -191.0998
  );

  const ruleTargetSegment = ['In', 'el', 'principio', 'la', 'Palabra', 'ya', 'existía', '.'];
  const ruleResult = new TranslationResult(
    sourceSegment,
    ruleTargetSegment,
    [1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0],
    [
      TranslationSources.Transfer,
      TranslationSources.None,
      TranslationSources.None,
      TranslationSources.None,
      TranslationSources.None,
      TranslationSources.None,
      TranslationSources.None,
      TranslationSources.None
    ],
    createAlignment(sourceSegment.length, ruleTargetSegment.length, [
      { i: 0, j: 0 },
      { i: 1, j: 1 },
      { i: 2, j: 2 },
      { i: 3, j: 3 },
      { i: 4, j: 4 },
      { i: 5, j: 5 },
      { i: 6, j: 6 },
      { i: 7, j: 7 }
    ]),
    []
  );

  const result = new HybridInteractiveTranslationResult(wordGraph, ruleResult);

  return new RemoteInteractiveTranslationSession(
    instance(mockedRestClient),
    new ErrorCorrectionModel(),
    'project01',
    1,
    sourceSegment,
    result
  );
}

interface AlignedWordPair {
  i: number;
  j: number;
}

function createArc(options: {
  prevState: number;
  nextState: number;
  score: number;
  words: string[];
  wordConfidences: number[];
  sourceSegmentRange: Range;
  isUnknown: boolean;
  alignment: AlignedWordPair[];
}): WordGraphArc {
  const alignment = createAlignment(options.sourceSegmentRange.length, options.words.length, options.alignment);
  return new WordGraphArc(
    options.prevState,
    options.nextState,
    options.score,
    options.words,
    alignment,
    options.sourceSegmentRange,
    options.isUnknown,
    options.wordConfidences
  );
}

function createAlignment(rowCount: number, columnCount: number, pairs: AlignedWordPair[]): WordAlignmentMatrix {
  const alignment = new WordAlignmentMatrix(rowCount, columnCount);
  for (const pair of pairs) {
    alignment.set(pair.i, pair.j, true);
  }
  return alignment;
}
