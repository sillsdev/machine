import { of, throwError } from 'rxjs';
import { deepEqual, instance, mock, when } from 'ts-mockito';
import { TranslationSources } from '../translation/translation-sources';
import { BuildDto } from './build-dto';
import { BuildStates } from './build-states';
import { EngineDto } from './engine-dto';
import { HttpClient } from './http-client';
import { InteractiveTranslationResultDto } from './interactive-translation-result-dto';
import { RxjsHttpClient } from './rxjs-http-client';
import { WebApiClient } from './web-api-client';

describe('WebApiClient', () => {
  it('translate interactively', async () => {
    const env = new TestEnvironment();
    const sourceSegment = ['Esto', 'es', 'una', 'prueba', '.'];
    when(
      env.mockedHttpClient.post<InteractiveTranslationResultDto>(
        'translation/engines/project:project01/actions/interactiveTranslate',
        deepEqual(sourceSegment)
      )
    ).thenReturn(
      of({
        status: 200,
        data: {
          wordGraph: {
            initialStateScore: -111.111,
            finalStates: [4],
            arcs: [
              {
                prevState: 0,
                nextState: 1,
                score: -11.11,
                words: ['This', 'is'],
                confidences: [0.4, 0.5],
                sourceSegmentRange: { start: 0, end: 2 },
                isUnknown: false,
                alignment: [{ sourceIndex: 0, targetIndex: 0 }, { sourceIndex: 1, targetIndex: 1 }]
              },
              {
                prevState: 1,
                nextState: 2,
                score: -22.22,
                words: ['a'],
                confidences: [0.6],
                sourceSegmentRange: { start: 2, end: 3 },
                isUnknown: false,
                alignment: [{ sourceIndex: 0, targetIndex: 0 }]
              },
              {
                prevState: 2,
                nextState: 3,
                score: 33.33,
                words: ['prueba'],
                confidences: [0],
                sourceSegmentRange: { start: 3, end: 4 },
                isUnknown: true,
                alignment: [{ sourceIndex: 0, targetIndex: 0 }]
              },
              {
                prevState: 3,
                nextState: 4,
                score: -44.44,
                words: ['.'],
                confidences: [0.7],
                sourceSegmentRange: { start: 4, end: 5 },
                isUnknown: false,
                alignment: [{ sourceIndex: 0, targetIndex: 0 }]
              }
            ]
          },
          ruleResult: {
            target: ['Esto', 'es', 'una', 'test', '.'],
            confidences: [0.0, 0.0, 0.0, 1.0, 0.0],
            sources: [
              TranslationSources.None,
              TranslationSources.None,
              TranslationSources.None,
              TranslationSources.Transfer,
              TranslationSources.None
            ],
            alignment: [
              { sourceIndex: 0, targetIndex: 0 },
              { sourceIndex: 1, targetIndex: 1 },
              { sourceIndex: 2, targetIndex: 2 },
              { sourceIndex: 3, targetIndex: 3 },
              { sourceIndex: 4, targetIndex: 4 }
            ],
            phrases: [
              { sourceSegmentRange: { start: 0, end: 3 }, targetSegmentCut: 3, confidence: 1 },
              { sourceSegmentRange: { start: 3, end: 4 }, targetSegmentCut: 4, confidence: 1 },
              { sourceSegmentRange: { start: 4, end: 5 }, targetSegmentCut: 5, confidence: 1 }
            ]
          }
        }
      })
    );

    const result = await env.client.translateInteractively('project01', sourceSegment);
    const wordGraph = result.smtWordGraph;
    expect(wordGraph.initialStateScore).toEqual(-111.111);
    expect(Array.from(wordGraph.finalStates)).toEqual([4]);
    expect(wordGraph.arcs.length).toEqual(4);
    let arc = wordGraph.arcs[0];
    expect(arc.prevState).toEqual(0);
    expect(arc.nextState).toEqual(1);
    expect(arc.score).toEqual(-11.11);
    expect(arc.words).toEqual(['This', 'is']);
    expect(arc.wordConfidences).toEqual([0.4, 0.5]);
    expect(arc.sourceSegmentRange.start).toEqual(0);
    expect(arc.sourceSegmentRange.end).toEqual(2);
    expect(arc.isUnknown).toBeFalsy();
    expect(arc.alignment.get(0, 0)).toBeTruthy();
    expect(arc.alignment.get(1, 1)).toBeTruthy();
    arc = wordGraph.arcs[2];
    expect(arc.isUnknown).toBeTruthy();

    const ruleResult = result.ruleResult;
    expect(ruleResult).not.toBeUndefined();
    if (ruleResult == null) {
      return;
    }
    expect(ruleResult.targetSegment).toEqual(['Esto', 'es', 'una', 'test', '.']);
    expect(ruleResult.wordConfidences).toEqual([0.0, 0.0, 0.0, 1.0, 0.0]);
    expect(ruleResult.wordSources).toEqual([
      TranslationSources.None,
      TranslationSources.None,
      TranslationSources.None,
      TranslationSources.Transfer,
      TranslationSources.None
    ]);
    expect(ruleResult.alignment.get(0, 0)).toBeTruthy();
    expect(ruleResult.alignment.get(1, 1)).toBeTruthy();
    expect(ruleResult.alignment.get(2, 2)).toBeTruthy();
    expect(ruleResult.alignment.get(3, 3)).toBeTruthy();
    expect(ruleResult.alignment.get(4, 4)).toBeTruthy();
  });

  it('translate interactively with no rule result', async () => {
    const env = new TestEnvironment();
    const sourceSegment = ['Esto', 'es', 'una', 'prueba', '.'];
    when(
      env.mockedHttpClient.post<InteractiveTranslationResultDto>(
        'translation/engines/project:project01/actions/interactiveTranslate',
        deepEqual(sourceSegment)
      )
    ).thenReturn(
      of({
        status: 200,
        data: {
          wordGraph: {
            initialStateScore: -111.111,
            finalStates: [],
            arcs: []
          }
        }
      })
    );

    const result = await env.client.translateInteractively('project01', sourceSegment);
    expect(result.smtWordGraph).not.toBeUndefined();
    expect(result.ruleResult).toBeUndefined();
  });

  it('train with no errors', () => {
    const env = new TestEnvironment();
    env.addCreateBuild();
    env.addBuildProgress();

    expect.assertions(12);
    let expectedStep = -1;
    env.client.train('project01').subscribe(
      progress => {
        expectedStep++;
        expect(progress.percentCompleted).toEqual(expectedStep / 10);
      },
      () => {},
      () => {
        expect(expectedStep).toEqual(10);
      }
    );
  });

  it('train with error while starting build', () => {
    const env = new TestEnvironment();
    when(env.mockedHttpClient.post<BuildDto>('translation/builds', 'engine01')).thenReturn(
      throwError(new Error('Error while creating build.'))
    );

    expect.assertions(1);
    env.client
      .train('project01')
      .subscribe(() => {}, err => expect(err.message).toEqual('Error while creating build.'));
  });

  it('train with error during build', () => {
    const env = new TestEnvironment();
    env.addCreateBuild();
    when(env.mockedHttpClient.get<BuildDto>(`translation/builds/id:build01?minRevision=1`)).thenReturn(
      of({
        status: 200,
        data: {
          id: 'build01',
          href: 'translation/builds/id:build01',
          revision: 1,
          engine: { id: 'engine01', href: 'translation/engines/id:engine01' },
          percentCompleted: 0.1,
          message: 'broken',
          state: BuildStates.Faulted
        }
      })
    );

    expect.assertions(2);
    env.client
      .train('project01')
      .subscribe(
        progress => expect(progress.percentCompleted).toEqual(0),
        err => expect(err.message).toEqual('Error occurred during build: broken')
      );
  });

  it('listen for training status with no errors', () => {
    const env = new TestEnvironment();
    when(env.mockedHttpClient.get<BuildDto>('translation/builds/engine:engine01?minRevision=0')).thenReturn(
      of({
        status: 200,
        data: {
          id: 'build01',
          href: 'translation/builds/id:build01',
          revision: 0,
          engine: { id: 'engine01', href: 'translation/engines/id:engine01' },
          percentCompleted: 0,
          message: '',
          state: BuildStates.Pending
        }
      })
    );
    env.addBuildProgress();

    expect.assertions(12);
    let expectedStep = -1;
    env.client.listenForTrainingStatus('project01').subscribe(
      progress => {
        expectedStep++;
        expect(progress.percentCompleted).toEqual(expectedStep / 10);
      },
      () => {},
      () => {
        expect(expectedStep).toEqual(10);
      }
    );
  });
});

class TestEnvironment {
  readonly mockedHttpClient: HttpClient;
  readonly client: WebApiClient;

  constructor() {
    this.mockedHttpClient = mock(RxjsHttpClient);
    when(this.mockedHttpClient.get<EngineDto>('translation/engines/project:project01')).thenReturn(
      of({
        status: 200,
        data: {
          id: 'engine01',
          href: 'translation/engines/id:engine01',
          sourceLanguageTag: 'en',
          targetLanguageTag: 'es',
          isShared: false,
          projects: [{ id: 'project01', href: 'translation/projects/id:project01' }],
          confidence: 0.2
        }
      })
    );
    this.client = new WebApiClient(instance(this.mockedHttpClient));
  }

  addCreateBuild(): void {
    when(this.mockedHttpClient.post<BuildDto>('translation/builds', JSON.stringify('engine01'))).thenReturn(
      of({
        status: 201,
        data: {
          id: 'build01',
          href: 'translation/builds/id:build01',
          revision: 0,
          engine: { id: 'engine01', href: 'translation/engines/id:engine01' },
          percentCompleted: 0,
          message: '',
          state: BuildStates.Pending
        }
      })
    );
  }

  addBuildProgress(): void {
    for (let i = 1; i <= 10; i++) {
      when(this.mockedHttpClient.get<BuildDto>(`translation/builds/id:build01?minRevision=${i}`)).thenReturn(
        of({
          status: 200,
          data: {
            id: 'build01',
            href: 'translation/builds/id:build01',
            revision: i,
            engine: { id: 'engine01', href: 'translation/engines/id:engine01' },
            percentCompleted: i / 10,
            message: '',
            state: i === 10 ? BuildStates.Completed : BuildStates.Active
          }
        })
      );
    }
  }
}
