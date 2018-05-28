using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Html5;
using Bridge.QUnit;
using Newtonsoft.Json;
using SIL.Machine.WebApi.Client;
using SIL.Machine.WebApi.Dtos;

namespace SIL.Machine.Translation
{
	public static class TranslationEngineTests
	{
		[Ready]
		public static void RunTests()
		{
			QUnit.Module(nameof(TranslationEngineTests));

			QUnit.Test(nameof(TranslateInteractively_Success), TranslateInteractively_Success);
			QUnit.Test(nameof(TranslateInteractively_Error), TranslateInteractively_Error);
			QUnit.Test(nameof(TranslateInteractively_NoRuleResult), TranslateInteractively_NoRuleResult);
			QUnit.Test(nameof(Train_NoError), Train_NoError);
			QUnit.Test(nameof(Train_ErrorCreatingBuild), Train_ErrorCreatingBuild);
			QUnit.Test(nameof(ListenForTrainingStatus_NoError), ListenForTrainingStatus_NoError);
			QUnit.Test(nameof(ListenForTrainingStatus_Close), ListenForTrainingStatus_Close);
		}

		private static void TranslateInteractively_Success(Assert assert)
		{
			var httpClient = new MockHttpClient();
			var resultDto = new InteractiveTranslationResultDto
			{
				WordGraph = new WordGraphDto
				{
					InitialStateScore = -111.111f,
					FinalStates = new[] { 4 },
					Arcs = new[]
					{
						new WordGraphArcDto
						{
							PrevState = 0,
							NextState = 1,
							Score = -11.11f,
							Words = new[] { "This", "is" },
							Confidences = new[] { 0.4f, 0.5f },
							SourceSegmentRange = new RangeDto { Start = 0, End = 2 },
							IsUnknown = false,
							Alignment = new[]
							{
								new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 },
								new AlignedWordPairDto { SourceIndex = 1, TargetIndex = 1 }
							}
						},
						new WordGraphArcDto
						{
							PrevState = 1,
							NextState = 2,
							Score = -22.22f,
							Words = new[] { "a" },
							Confidences = new[] { 0.6f },
							SourceSegmentRange = new RangeDto { Start = 2, End = 3 },
							IsUnknown = false,
							Alignment = new[]
							{
								new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
							}
						},
						new WordGraphArcDto
						{
							PrevState = 2,
							NextState = 3,
							Score = 33.33f,
							Words = new[] { "prueba" },
							Confidences = new[] { 0.0f },
							SourceSegmentRange = new RangeDto { Start = 3, End = 4 },
							IsUnknown = true,
							Alignment = new[]
							{
								new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
							}
						},
						new WordGraphArcDto
						{
							PrevState = 3,
							NextState = 4,
							Score = -44.44f,
							Words = new[] { "." },
							Confidences = new[] { 0.7f },
							SourceSegmentRange = new RangeDto { Start = 4, End = 5 },
							IsUnknown = false,
							Alignment = new[]
							{
								new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
							}
						}
					}
				},
				RuleResult = new TranslationResultDto
				{
					Target = new[] { "Esto", "es", "una", "test", "." },
					Confidences = new[] { 0.0f, 0.0f, 0.0f, 1.0f, 0.0f },
					Sources = new[]
					{
						TranslationSources.None,
						TranslationSources.None,
						TranslationSources.None,
						TranslationSources.Transfer,
						TranslationSources.None
					},
					Alignment = new[]
					{
						new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 },
						new AlignedWordPairDto { SourceIndex = 1, TargetIndex = 1 },
						new AlignedWordPairDto { SourceIndex = 2, TargetIndex = 2 },
						new AlignedWordPairDto { SourceIndex = 3, TargetIndex = 3 },
						new AlignedWordPairDto { SourceIndex = 4, TargetIndex = 4 }
					}
				}
			};
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Post,
					ResponseText = JsonConvert.SerializeObject(resultDto, RestClientBase.SerializerSettings)
				});

			var engine = new TranslationEngine("http://localhost/", "project1", httpClient);
			Action done = assert.Async();
			engine.TranslateInteractively("Esto es una prueba.", 0.2, session =>
				{
					assert.NotEqual(session, null);

					WordGraph wordGraph = session.SmtWordGraph;
					assert.Equal(wordGraph.InitialStateScore, -111.111);
					assert.DeepEqual(wordGraph.FinalStates.ToArray(), new[] { 4 });
					assert.Equal(wordGraph.Arcs.Count, 4);
					WordGraphArc arc = wordGraph.Arcs[0];
					assert.Equal(arc.PrevState, 0);
					assert.Equal(arc.NextState, 1);
					assert.Equal(arc.Score, -11.11);
					assert.DeepEqual(arc.Words.ToArray(), new[] { "This", "is" });
					assert.DeepEqual(arc.WordConfidences.ToArray(), new[] { 0.4, 0.5 });
					assert.Equal(arc.SourceSegmentRange.Start, 0);
					assert.Equal(arc.SourceSegmentRange.End, 2);
					assert.Equal(arc.IsUnknown, false);
					assert.Equal(arc.Alignment[0, 0], true);
					assert.Equal(arc.Alignment[1, 1], true);
					arc = wordGraph.Arcs[2];
					assert.Equal(arc.IsUnknown, true);

					TranslationResult ruleResult = session.RuleResult;
					assert.DeepEqual(ruleResult.TargetSegment.ToArray(), new[] { "Esto", "es", "una", "test", "." });
					assert.DeepEqual(ruleResult.WordConfidences.ToArray(), new[] { 0.0, 0.0, 0.0, 1.0, 0.0 });
					assert.DeepEqual(ruleResult.WordSources.ToArray(),
						new[]
						{
							TranslationSources.None,
							TranslationSources.None,
							TranslationSources.None,
							TranslationSources.Transfer,
							TranslationSources.None
						});
					assert.Equal(ruleResult.Alignment[0, 0], true);
					assert.Equal(ruleResult.Alignment[1, 1], true);
					assert.Equal(ruleResult.Alignment[2, 2], true);
					assert.Equal(ruleResult.Alignment[3, 3], true);
					assert.Equal(ruleResult.Alignment[4, 4], true);
					done();
				});
		}

		private static void TranslateInteractively_Error(Assert assert)
		{
			var httpClient = new MockHttpClient();
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Post,
					ErrorStatus = 404
				});

			var engine = new TranslationEngine("http://localhost/", "project1", httpClient);
			Action done = assert.Async();
			engine.TranslateInteractively("Esto es una prueba.", 0.2, session =>
				{
					assert.Equal(session, null);
					done();
				});
		}

		private static void TranslateInteractively_NoRuleResult(Assert assert)
		{
			var httpClient = new MockHttpClient();
			var resultDto = new InteractiveTranslationResultDto
			{
				WordGraph = new WordGraphDto
				{
					InitialStateScore = -111.111f,
					FinalStates = new int[0],
					Arcs = new WordGraphArcDto[0]
				},
				RuleResult = null
			};
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Post,
					ResponseText = JsonConvert.SerializeObject(resultDto, RestClientBase.SerializerSettings)
				});

			var engine = new TranslationEngine("http://localhost/", "project1", httpClient);
			Action done = assert.Async();
			engine.TranslateInteractively("Esto es una prueba.", 0.2, session =>
				{
					assert.NotEqual(session, null);
					assert.NotEqual(session.SmtWordGraph, null);
					assert.Equal(session.RuleResult, null);
					done();
				});
		}

		private static void Train_NoError(Assert assert)
		{
			var httpClient = new MockHttpClient();
			var engineDto = new EngineDto { Id = "engine1" };
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Get,
					Url = "translation/engines/project:project1",
					ResponseText = JsonConvert.SerializeObject(engineDto, RestClientBase.SerializerSettings)
				});
			var buildDto = new BuildDto { Id = "build1" };
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Post,
					Url = "translation/builds",
					ResponseText = JsonConvert.SerializeObject(buildDto, RestClientBase.SerializerSettings)
				});
			for (int i = 0; i < 10; i++)
			{
				buildDto.PercentCompleted = (double) (i + 1) / 10;
				buildDto.Revision++;
				httpClient.Requests.Add(new MockRequest
					{
						Method = HttpRequestMethod.Get,
						Url = string.Format("translation/builds/id:build1?minRevision={0}", buildDto.Revision),
						Action = (body, ct) => Delay(10, ct),
						ResponseText = JsonConvert.SerializeObject(buildDto, RestClientBase.SerializerSettings)
					});
			}
			var engine = new TranslationEngine("http://localhost/", "project1", httpClient);
			Action done = assert.Async();
			int expectedStep = -1;
			engine.Train(
				progress =>
				{
					expectedStep++;
					assert.Equal(progress.PercentCompleted, (double) expectedStep / 10);
				},
				resultCode =>
				{
					assert.Equal(expectedStep, 10);
					assert.Equal(resultCode, TrainResultCode.NoError);
					done();
				});
		}

		private static void Train_ErrorCreatingBuild(Assert assert)
		{
			var httpClient = new MockHttpClient();
			var engineDto = new EngineDto
			{
				Id = "engine1"
			};
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Get,
					Url = "translation/engines/project:project1",
					ResponseText = JsonConvert.SerializeObject(engineDto, RestClientBase.SerializerSettings)
				});
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Post,
					Url = "translation/builds",
					ErrorStatus = 500
				});
			var engine = new TranslationEngine("http://localhost/", "project1", httpClient);
			Action done = assert.Async();
			engine.Train(
				progress => {},
				resultCode =>
				{
					assert.Equal(resultCode, TrainResultCode.HttpError);
					done();
				});
		}

		private static void ListenForTrainingStatus_NoError(Assert assert)
		{
			var httpClient = new MockHttpClient();
			var engineDto = new EngineDto { Id = "engine1" };
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Get,
					Url = "translation/engines/project:project1",
					ResponseText = JsonConvert.SerializeObject(engineDto, RestClientBase.SerializerSettings)
				});
			var buildDto = new BuildDto { Id = "build1" };
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Get,
					Url = "translation/builds/engine:engine1?minRevision=0",
					Action = (body, ct) => Delay(10, ct),
					ResponseText = JsonConvert.SerializeObject(buildDto, RestClientBase.SerializerSettings)
				});
			for (int i = 0; i < 10; i++)
			{
				buildDto.PercentCompleted = (double) (i + 1) / 10;
				buildDto.Revision++;
				httpClient.Requests.Add(new MockRequest
					{
						Method = HttpRequestMethod.Get,
						Url = string.Format("translation/builds/id:build1?minRevision={0}", buildDto.Revision),
						Action = (body, ct) => Delay(10, ct),
						ResponseText = JsonConvert.SerializeObject(buildDto, RestClientBase.SerializerSettings)
					});
			}
			var engine = new TranslationEngine("http://localhost/", "project1", httpClient);
			Action done = assert.Async();
			int expectedStep = -1;
			engine.ListenForTrainingStatus(
				progress =>
				{
					expectedStep++;
					assert.Equal(progress.PercentCompleted, (double) expectedStep / 10);
				},
				resultCode =>
				{
					assert.Equal(expectedStep, 10);
					assert.Equal(resultCode, TrainResultCode.NoError);
					done();
				});
		}

		private static void ListenForTrainingStatus_Close(Assert assert)
		{
			var httpClient = new MockHttpClient();
			var engineDto = new EngineDto { Id = "engine1" };
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Get,
					Url = "translation/engines/project:project1",
					ResponseText = JsonConvert.SerializeObject(engineDto, RestClientBase.SerializerSettings)
				});
			var buildDto = new BuildDto { Id = "build1" };
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Get,
					Url = "translation/builds/engine:engine1?minRevision=0",
					Action = (body, ct) => Delay(1000, ct),
					ResponseText = JsonConvert.SerializeObject(buildDto, RestClientBase.SerializerSettings)
				});
			var engine = new TranslationEngine("http://localhost/", "project1", httpClient);
			Action done = assert.Async();
			engine.ListenForTrainingStatus(
				progress => { },
				resultCode =>
				{
					assert.Equal(resultCode, TrainResultCode.NoError);
					done();
				});
			engine.Close();
		}

		private static Task Delay(int milliseconds, CancellationToken ct)
		{
			var tcs = new TaskCompletionSource<bool>();

			int id = Global.SetTimeout(() => tcs.TrySetResult(true), milliseconds);

			CancellationTokenRegistration reg = ct.Register(() =>
			{
				if (tcs.TrySetCanceled())
					Global.ClearTimeout(id);
			});
			tcs.Task.ContinueWith(_ => reg.Dispose());
			return tcs.Task;
		}
	}
}
