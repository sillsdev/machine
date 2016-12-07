using System.Linq;
using Bridge.Html5;
using Bridge.QUnit;
using SIL.Machine.JS.Tests.Web;
using SIL.Machine.Translation;

namespace SIL.Machine.JS.Tests.Translation
{
	public static class TranslationEngineTests
	{
		[Ready]
		public static void RunTests()
		{
			QUnit.Module(nameof(TranslationEngineTests));

			QUnit.Test(nameof(GetSuggester_Success_ReturnsSuggester), GetSuggester_Success_ReturnsSuggester);
			QUnit.Test(nameof(GetSuggester_Error_ReturnsNull), GetSuggester_Error_ReturnsNull);
		}

		private static void GetSuggester_Success_ReturnsSuggester(Assert assert)
		{
			var webClient = new MockWebClient();
			dynamic json = new
			{
				wordGraph = new
				{
					initialStateScore = -111.111,
					finalStates = new [] {4},
					arcs = new[]
					{
						new
						{
							prevState = 0,
							nextState = 1,
							score = -11.11,
							words = new[] {"This", "is"},
							confidences = new[] {0.4, 0.5},
							sourceStartIndex = 0,
							sourceEndIndex = 1,
							isUnknown = false,
							alignment = new[]
							{
								new {sourceIndex = 0, targetIndex = 0},
								new {sourceIndex = 1, targetIndex = 1}
							}
						},
						new
						{
							prevState = 1,
							nextState = 2,
							score = -22.22,
							words = new[] {"a"},
							confidences = new[] {0.6},
							sourceStartIndex = 2,
							sourceEndIndex = 2,
							isUnknown = false,
							alignment = new[]
							{
								new {sourceIndex = 0, targetIndex = 0}
							}
						},
						new
						{
							prevState = 2,
							nextState = 3,
							score = 33.33,
							words = new[] {"prueba"},
							confidences = new[] {0.0},
							sourceStartIndex = 3,
							sourceEndIndex = 3,
							isUnknown = true,
							alignment = new[]
							{
								new {sourceIndex = 0, targetIndex = 0}
							}
						},
						new
						{
							prevState = 3,
							nextState = 4,
							score = -44.44,
							words = new[] {"."},
							confidences = new[] {0.7},
							sourceStartIndex = 4,
							sourceEndIndex = 4,
							isUnknown = false,
							alignment = new[]
							{
								new {sourceIndex = 0, targetIndex = 0}
							}
						}
					}
				},
				ruleResult = new
				{
					target = new[] {"Esto", "es", "una", "test", "."},
					confidences = new[] {0.0, 0.0, 0.0, 1.0, 0.0},
					alignment = new[]
					{
						new {sourceIndex = 0, targetIndex = 0, sources = 0},
						new {sourceIndex = 1, targetIndex = 1, sources = 0},
						new {sourceIndex = 2, targetIndex = 2, sources = 0},
						new {sourceIndex = 3, targetIndex = 3, sources = 2},
						new {sourceIndex = 4, targetIndex = 4, sources = 0}
					}
				}
			};
			webClient.Requests.Add(new MockRequest {ResponseText = JSON.Stringify(json)});

			var engine = new TranslationEngine("http://localhost", "es", "en", webClient);
			engine.GetSuggester("Esto es una prueba .".Split(" "), suggester =>
				{
					assert.NotEqual(suggester, null);

					WordGraph wordGraph = suggester.SmtWordGraph;
					assert.Equal(wordGraph.InitialStateScore, -111.111);
					assert.DeepEqual(wordGraph.FinalStates.ToArray(), new[] {4});
					assert.Equal(wordGraph.Arcs.Count, 4);
					WordGraphArc arc = wordGraph.Arcs[0];
					assert.Equal(arc.PrevState, 0);
					assert.Equal(arc.NextState, 1);
					assert.Equal(arc.Score, -11.11);
					assert.DeepEqual(arc.Words.ToArray(), new[] {"This", "is"});
					assert.DeepEqual(arc.WordConfidences.ToArray(), new[] {0.4, 0.5});
					assert.Equal(arc.SourceStartIndex, 0);
					assert.Equal(arc.SourceEndIndex, 1);
					assert.Equal(arc.IsUnknown, false);
					assert.Equal(arc.Alignment[0, 0], AlignmentType.Aligned);
					assert.Equal(arc.Alignment[1, 1], AlignmentType.Aligned);

					TranslationResult ruleResult = suggester.RuleResult;
					assert.DeepEqual(ruleResult.TargetSegment.ToArray(), new[] {"Esto", "es", "una", "test", "."});
					assert.DeepEqual(ruleResult.TargetWordConfidences.ToArray(), new[] {0.0, 0.0, 0.0, 1.0, 0.0});
					AlignedWordPair wordPair;
					assert.Ok(ruleResult.TryGetWordPair(0, 0, out wordPair));
					assert.Equal(wordPair.Sources, TranslationSources.None);
					assert.Ok(ruleResult.TryGetWordPair(3, 3, out wordPair));
					assert.Equal(wordPair.Sources, TranslationSources.Transfer);
				});
		}

		private static void GetSuggester_Error_ReturnsNull(Assert assert)
		{
			var webClient = new MockWebClient();
			webClient.Requests.Add(new MockRequest {ErrorStatus = 404});

			var engine = new TranslationEngine("http://localhost", "es", "en", webClient);
			engine.GetSuggester("Esto es una prueba .".Split(" "), suggester =>
				{
					assert.Equal(suggester, null);
				});
		}
	}
}
