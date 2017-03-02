using Bridge.Html5;
using Bridge.QUnit;
using SIL.Machine.JS.Tests.Web;
using SIL.Machine.Translation;

namespace SIL.Machine.JS.Tests.Translation
{
	public static class InteractiveTranslationSessionTests
	{
		private static readonly dynamic Json = new
		{
			wordGraph = new
			{
				initialStateScore = -191.0998,
				finalStates = new [] {12, 13, 14, 15, 16, 17, 18, 19, 20, 21},
				arcs = new[]
				{
					new
					{
						prevState = 0,
						nextState = 1,
						score = -22.4162,
						words = new[] {"now", "It"},
						confidences = new[] {0.00006755903, 0.0116618536},
						sourceStartIndex = 0,
						sourceEndIndex = 1,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 1},
							new {sourceIndex = 1, targetIndex = 0}
						}
					},
					new
					{
						prevState = 0,
						nextState = 2,
						score = -23.5761,
						words = new[] {"In", "your"},
						confidences = new[] {0.355293363, 0.0000941652761},
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
						prevState = 0,
						nextState = 3,
						score = -11.1167,
						words = new[] {"In", "the"},
						confidences = new[] {0.355293363, 0.5004668},
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
						prevState = 0,
						nextState = 4,
						score = -13.7804,
						words = new[] {"In"},
						confidences = new[] {0.355293363},
						sourceStartIndex = 0,
						sourceEndIndex = 0,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 3,
						nextState = 5,
						score = -12.9695,
						words = new[] {"beginning"},
						confidences = new[] {0.348795831},
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
						prevState = 4,
						nextState = 5,
						score = -7.68319,
						words = new[] {"the", "beginning"},
						confidences = new[] {0.5004668, 0.348795831},
						sourceStartIndex = 1,
						sourceEndIndex = 2,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0},
							new {sourceIndex = 1, targetIndex = 1}
						}
					},
					new
					{
						prevState = 4,
						nextState = 3,
						score = -14.4373,
						words = new[] {"the"},
						confidences = new[] {0.5004668},
						sourceStartIndex = 1,
						sourceEndIndex = 1,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 5,
						nextState = 6,
						score = -19.3042,
						words = new[] {"his", "Word"},
						confidences = new[] {0.00347203249, 0.477621228},
						sourceStartIndex = 3,
						sourceEndIndex = 4,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0},
							new {sourceIndex = 1, targetIndex = 1}
						}
					},
					new
					{
						prevState = 5,
						nextState = 7,
						score = -8.49148,
						words = new[] {"the", "Word"},
						confidences = new[] {0.346071422, 0.477621228},
						sourceStartIndex = 3,
						sourceEndIndex = 4,
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
						nextState = 8,
						score = -15.2926,
						words = new[] {"beginning"},
						confidences = new[] {0.348795831},
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
						nextState = 9,
						score = -15.2926,
						words = new[] {"beginning"},
						confidences = new[] {0.348795831},
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
						prevState = 7,
						nextState = 10,
						score = -14.3453,
						words = new[] {"already"},
						confidences = new[] {0.2259867},
						sourceStartIndex = 5,
						sourceEndIndex = 5,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 8,
						nextState = 6,
						score = -19.3042,
						words = new[] {"his", "Word"},
						confidences = new[] {0.00347203249, 0.477621228},
						sourceStartIndex = 3,
						sourceEndIndex = 4,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0},
							new {sourceIndex = 1, targetIndex = 1}
						}
					},
					new
					{
						prevState = 8,
						nextState = 7,
						score = -8.49148,
						words = new[] {"the", "Word"},
						confidences = new[] {0.346071422, 0.477621228},
						sourceStartIndex = 3,
						sourceEndIndex = 4,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0},
							new {sourceIndex = 1, targetIndex = 1}
						}
					},
					new
					{
						prevState = 9,
						nextState = 6,
						score = -19.3042,
						words = new[] {"his", "Word"},
						confidences = new[] {0.00347203249, 0.477621228},
						sourceStartIndex = 3,
						sourceEndIndex = 4,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0},
							new {sourceIndex = 1, targetIndex = 1}
						}
					},
					new
					{
						prevState = 9,
						nextState = 7,
						score = -8.49148,
						words = new[] {"the", "Word"},
						confidences = new[] {0.346071422, 0.477621228},
						sourceStartIndex = 3,
						sourceEndIndex = 4,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0},
							new {sourceIndex = 1, targetIndex = 1}
						}
					},
					new
					{
						prevState = 6,
						nextState = 10,
						score = -14.0526,
						words = new[] {"already"},
						confidences = new[] {0.2259867},
						sourceStartIndex = 5,
						sourceEndIndex = 5,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 10,
						nextState = 11,
						score = 51.1117,
						words = new[] {"existía"},
						confidences = new[] {0.0},
						sourceStartIndex = 6,
						sourceEndIndex = 6,
						isUnknown = true,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 11,
						nextState = 12,
						score = -29.0049,
						words = new[] {"you", "."},
						confidences = new[] {0.005803475, 0.317073762},
						sourceStartIndex = 7,
						sourceEndIndex = 7,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 1}
						}
					},
					new
					{
						prevState = 11,
						nextState = 13,
						score = -27.7143,
						words = new[] {"to"},
						confidences = new[] {0.038961038},
						sourceStartIndex = 7,
						sourceEndIndex = 7,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 11,
						nextState = 14,
						score = -30.0868,
						words = new[] {".", "‘"},
						confidences = new[] {0.317073762, 0.06190489},
						sourceStartIndex = 7,
						sourceEndIndex = 7,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 11,
						nextState = 15,
						score = -30.1586,
						words = new[] {".", "he"},
						confidences = new[] {0.317073762, 0.06702433},
						sourceStartIndex = 7,
						sourceEndIndex = 7,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 11,
						nextState = 16,
						score = -28.2444,
						words = new[] {".", "the"},
						confidences = new[] {0.317073762, 0.115540564},
						sourceStartIndex = 7,
						sourceEndIndex = 7,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 11,
						nextState = 17,
						score = -23.8056,
						words = new[] {"and"},
						confidences = new[] {0.08047272},
						sourceStartIndex = 7,
						sourceEndIndex = 7,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 11,
						nextState = 18,
						score = -23.5842,
						words = new[] {"the"},
						confidences = new[] {0.09361572},
						sourceStartIndex = 7,
						sourceEndIndex = 7,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 11,
						nextState = 19,
						score = -18.8988,
						words = new[] {","},
						confidences = new[] {0.1428188},
						sourceStartIndex = 7,
						sourceEndIndex = 7,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 11,
						nextState = 20,
						score = -11.9218,
						words = new[] {".", "’"},
						confidences = new[] {0.317073762, 0.018057242},
						sourceStartIndex = 7,
						sourceEndIndex = 7,
						isUnknown = false,
						alignment = new[]
						{
							new {sourceIndex = 0, targetIndex = 0}
						}
					},
					new
					{
						prevState = 11,
						nextState = 21,
						score = -3.51852,
						words = new[] {"."},
						confidences = new[] {0.317073762},
						sourceStartIndex = 7,
						sourceEndIndex = 7,
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
				target = new[] {"In", "el", "principio", "la", "Palabra", "ya", "existía", "."},
				confidences = new[] {1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0},
				sources = new[]
				{
					TranslationSources.Transfer,
					TranslationSources.None,
					TranslationSources.None,
					TranslationSources.None,
					TranslationSources.None,
					TranslationSources.None,
					TranslationSources.None,
					TranslationSources.None
				},
				alignment = new[]
				{
					new {sourceIndex = 0, targetIndex = 0},
					new {sourceIndex = 1, targetIndex = 1},
					new {sourceIndex = 2, targetIndex = 2},
					new {sourceIndex = 3, targetIndex = 3},
					new {sourceIndex = 4, targetIndex = 4},
					new {sourceIndex = 5, targetIndex = 5},
					new {sourceIndex = 6, targetIndex = 6},
					new {sourceIndex = 7, targetIndex = 7}
				}
			}
		};

		private static MockWebClient CreateWebClient()
		{
			var webClient = new MockWebClient();
			webClient.Requests.Add(new MockRequest {Url = "http://localhost/translation/engines/es/en/projects/project1/actions/interactive-translate", ResponseText = JSON.Stringify(Json)});
			return webClient;
		}

		[Ready]
		public static void RunTests()
		{
			QUnit.Module(nameof(InteractiveTranslationSessionTests));

			QUnit.Test(nameof(CurrentSuggestion_EmptyPrefix_ReturnsSuggestion), CurrentSuggestion_EmptyPrefix_ReturnsSuggestion);
			QUnit.Test(nameof(UpdatePrefix_AddOneCompleteWord_ReturnsSuggestion), UpdatePrefix_AddOneCompleteWord_ReturnsSuggestion);
			QUnit.Test(nameof(UpdatePrefix_AddOnePartialWord_ReturnsSuggestion), UpdatePrefix_AddOnePartialWord_ReturnsSuggestion);
			QUnit.Test(nameof(UpdatePrefix_RemoveOneWord_ReturnsSuggestion), UpdatePrefix_RemoveOneWord_ReturnsSuggestion);
			QUnit.Test(nameof(UpdatePrefix_RemoveEntirePrefix_ReturnsSuggestion), UpdatePrefix_RemoveEntirePrefix_ReturnsSuggestion);
			QUnit.Test(nameof(Approve_Success_ReturnsTrue), Approve_Success_ReturnsTrue);
			QUnit.Test(nameof(Approve_Error_ReturnsFalse), Approve_Error_ReturnsFalse);
		}

		private static void CurrentSuggestion_EmptyPrefix_ReturnsSuggestion(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost:64638", "es", "en", "project1");
			engine.TranslateInteractively("En el principio la Palabra ya existía .".Split(" "), 0.2, session =>
				{
					assert.NotEqual(session, null);

					assert.DeepEqual(session.CurrentSuggestion, "In the beginning the Word already".Split(" "));
				});
		}

		private static void UpdatePrefix_AddOneCompleteWord_ReturnsSuggestion(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost", "es", "en", "project1", CreateWebClient());
			engine.TranslateInteractively("En el principio la Palabra ya existía .".Split(" "), 0.2, session =>
				{
					assert.NotEqual(session, null);

					assert.DeepEqual(session.UpdatePrefix("In".Split(" "), true), "the beginning the Word already".Split(" "));
				});
		}

		private static void UpdatePrefix_AddOnePartialWord_ReturnsSuggestion(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost", "es", "en", "project1", CreateWebClient());
			engine.TranslateInteractively("En el principio la Palabra ya existía .".Split(" "), 0.2, session =>
				{
					assert.NotEqual(session, null);
					session.UpdatePrefix("In".Split(" "), true);

					assert.DeepEqual(session.UpdatePrefix("In t".Split(" "), false), "the beginning the Word already".Split(" "));
				});
		}

		private static void UpdatePrefix_RemoveOneWord_ReturnsSuggestion(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost", "es", "en", "project1", CreateWebClient());
			engine.TranslateInteractively("En el principio la Palabra ya existía .".Split(" "), 0.2, session =>
				{
					assert.NotEqual(session, null);
					session.UpdatePrefix("In the beginning".Split(" "), true);

					assert.DeepEqual(session.UpdatePrefix("In the".Split(" "), true), "beginning the Word already".Split(" "));
				});
		}

		private static void UpdatePrefix_RemoveEntirePrefix_ReturnsSuggestion(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost", "es", "en", "project1", CreateWebClient());
			engine.TranslateInteractively("En el principio la Palabra ya existía .".Split(" "), 0.2, session =>
				{
					assert.NotEqual(session, null);
					session.UpdatePrefix("In the beginning".Split(" "), true);

					assert.DeepEqual(session.UpdatePrefix("".Split(" "), true), "In the beginning the Word already".Split(" "));
				});
		}

		private static void Approve_Success_ReturnsTrue(Assert assert)
		{
			string[] sourceSegment = "En el principio la Palabra ya existía .".Split(" ");
			string[] prefix = "In the beginning the Word already existed .".Split(" ");

			MockWebClient webClient = CreateWebClient();
			webClient.Requests.Add(new MockRequest
				{
					Url = "http://localhost/translation/engines/es/en/projects/project1/actions/train-segment",
					CheckBody = body =>
					{
						dynamic json = JSON.Parse(body);
						assert.DeepEqual(json["sourceSegment"], sourceSegment);
						assert.DeepEqual(json["targetSegment"], prefix);
					},
					ResponseText = ""
				});

			var engine = new TranslationEngine("http://localhost", "es", "en", "project1", webClient);
			engine.TranslateInteractively(sourceSegment, 0.2, session =>
				{
					assert.NotEqual(session, null);
					session.UpdatePrefix(prefix, true);

					session.Approve(success => assert.Ok(success));
				});
		}

		private static void Approve_Error_ReturnsFalse(Assert assert)
		{
			MockWebClient webClient = CreateWebClient();
			webClient.Requests.Add(new MockRequest {Url = "http://localhost/translation/engines/es/en/projects/project1/actions/train-segment", ErrorStatus = 404});

			var engine = new TranslationEngine("http://localhost", "es", "en", "project1", webClient);
			engine.TranslateInteractively("En el principio la Palabra ya existía .".Split(" "), 0.2, session =>
				{
					assert.NotEqual(session, null);
					session.UpdatePrefix("In the beginning the Word already existed .".Split(" "), true);

					session.Approve(success => assert.NotOk(success));
				});
		}
	}
}
