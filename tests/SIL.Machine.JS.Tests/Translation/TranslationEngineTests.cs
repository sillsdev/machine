using System;
using System.Linq;
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

			QUnit.Test(nameof(TranslateInteractively_Success_ReturnsSession),
				TranslateInteractively_Success_ReturnsSession);
			QUnit.Test(nameof(TranslateInteractively_Error_ReturnsNull), TranslateInteractively_Error_ReturnsNull);
			QUnit.Test(nameof(TranslateInteractively_NoRuleResult_ReturnsSession),
				TranslateInteractively_NoRuleResult_ReturnsSession);
			QUnit.Test(nameof(Train_NoErrors_ReturnsTrue), Train_NoErrors_ReturnsTrue);
			QUnit.Test(nameof(Train_ErrorCreatingBuild_ReturnsFalse), Train_ErrorCreatingBuild_ReturnsFalse);
			QUnit.Test(nameof(ListenForTrainingStatus_NoErrors_ReturnsTrue),
				ListenForTrainingStatus_NoErrors_ReturnsTrue);
		}

		private static void TranslateInteractively_Success_ReturnsSession(Assert assert)
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
							SourceStartIndex = 0,
							SourceEndIndex = 1,
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
							SourceStartIndex = 2,
							SourceEndIndex = 2,
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
							SourceStartIndex = 3,
							SourceEndIndex = 3,
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
							SourceStartIndex = 4,
							SourceEndIndex = 4,
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
					assert.Equal(arc.SourceStartIndex, 0);
					assert.Equal(arc.SourceEndIndex, 1);
					assert.Equal(arc.IsUnknown, false);
					assert.Equal(arc.Alignment[0, 0], AlignmentType.Aligned);
					assert.Equal(arc.Alignment[1, 1], AlignmentType.Aligned);
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
					assert.Equal(ruleResult.Alignment[0, 0], AlignmentType.Aligned);
					assert.Equal(ruleResult.Alignment[1, 1], AlignmentType.Aligned);
					assert.Equal(ruleResult.Alignment[2, 2], AlignmentType.Aligned);
					assert.Equal(ruleResult.Alignment[3, 3], AlignmentType.Aligned);
					assert.Equal(ruleResult.Alignment[4, 4], AlignmentType.Aligned);
					done();
				});
		}

		private static void TranslateInteractively_Error_ReturnsNull(Assert assert)
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

		private static void TranslateInteractively_NoRuleResult_ReturnsSession(Assert assert)
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

		private static void Train_NoErrors_ReturnsTrue(Assert assert)
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
			var buildDto = new BuildDto
			{
				Id = "build1",
				StepCount = 10
			};
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Post,
					Url = "translation/builds",
					ResponseText = JsonConvert.SerializeObject(buildDto, RestClientBase.SerializerSettings)
				});
			for (int i = 0; i < 10; i++)
			{
				buildDto.CurrentStep++;
				buildDto.Revision++;
				httpClient.Requests.Add(new MockRequest
					{
						Method = HttpRequestMethod.Get,
						Url = string.Format("translation/builds/id:build1?minRevision={0}", buildDto.Revision),
						Action = async body => await Task.Delay(10),
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
					assert.Equal(progress.CurrentStep, expectedStep);
				},
				success =>
				{
					assert.Equal(expectedStep, 10);
					assert.Equal(success, true);
					done();
				});
		}

		private static void Train_ErrorCreatingBuild_ReturnsFalse(Assert assert)
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
				success =>
				{
					assert.Equal(success, false);
					done();
				});
		}

		private static void ListenForTrainingStatus_NoErrors_ReturnsTrue(Assert assert)
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
			var buildDto = new BuildDto
			{
				Id = "build1",
				StepCount = 10
			};
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Get,
					Url = "translation/builds/engine:engine1?waitNew=true",
					Action = async body => await Task.Delay(10),
					ResponseText = JsonConvert.SerializeObject(buildDto, RestClientBase.SerializerSettings)
				});
			for (int i = 0; i < 10; i++)
			{
				buildDto.CurrentStep++;
				buildDto.Revision++;
				httpClient.Requests.Add(new MockRequest
					{
						Method = HttpRequestMethod.Get,
						Url = string.Format("translation/builds/id:build1?minRevision={0}", buildDto.Revision),
						Action = async body => await Task.Delay(10),
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
					assert.Equal(progress.CurrentStep, expectedStep);
				},
				success =>
				{
					assert.Equal(expectedStep, 10);
					assert.Equal(success, true);
					done();
				});
		}
	}
}
