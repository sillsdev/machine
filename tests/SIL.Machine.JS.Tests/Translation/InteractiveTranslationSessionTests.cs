using System;
using System.Linq;
using Bridge.Html5;
using Bridge.QUnit;
using Newtonsoft.Json;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.Client;
using SIL.Machine.WebApi.Dtos;

namespace SIL.Machine.Translation
{
	public static class InteractiveTranslationSessionTests
	{
		private static readonly InteractiveTranslationResultDto ResultDto = new InteractiveTranslationResultDto
		{
			WordGraph = new WordGraphDto
			{
				InitialStateScore = -191.0998f,
				FinalStates = new[] { 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 },
				Arcs = new[]
				{
					new WordGraphArcDto
					{
						PrevState = 0,
						NextState = 1,
						Score = -22.4162f,
						Words = new[] { "now", "It" },
						Confidences = new[] { 0.00006755903f, 0.0116618536f },
						SourceSegmentRange = new RangeDto() { Start = 0, End = 2 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 1 },
							new AlignedWordPairDto { SourceIndex = 1, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 0,
						NextState = 2,
						Score = -23.5761f,
						Words = new[] { "In", "your" },
						Confidences = new[] { 0.355293363f, 0.0000941652761f },
						SourceSegmentRange = new RangeDto() { Start = 0, End = 2 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 },
							new AlignedWordPairDto { SourceIndex = 1, TargetIndex = 1 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 0,
						NextState = 3,
						Score = -11.1167f,
						Words = new[] { "In", "the" },
						Confidences = new[] { 0.355293363f, 0.5004668f },
						SourceSegmentRange = new RangeDto() { Start = 0, End = 2 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 },
							new AlignedWordPairDto { SourceIndex = 1, TargetIndex = 1 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 0,
						NextState = 4,
						Score = -13.7804f,
						Words = new[] { "In" },
						Confidences = new[] { 0.355293363f },
						SourceSegmentRange = new RangeDto() { Start = 0, End = 1 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 3,
						NextState = 5,
						Score = -12.9695f,
						Words = new[] { "beginning" },
						Confidences = new[] { 0.348795831f },
						SourceSegmentRange = new RangeDto() { Start = 2, End = 3 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 4,
						NextState = 5,
						Score = -7.68319f,
						Words = new[] { "the", "beginning" },
						Confidences = new[] { 0.5004668f, 0.348795831f },
						SourceSegmentRange = new RangeDto() { Start = 1, End = 3 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 },
							new AlignedWordPairDto { SourceIndex = 1, TargetIndex = 1 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 4,
						NextState = 3,
						Score = -14.4373f,
						Words = new[] { "the" },
						Confidences = new[] { 0.5004668f },
						SourceSegmentRange = new RangeDto() { Start = 1, End = 2 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 5,
						NextState = 6,
						Score = -19.3042f,
						Words = new[] { "his", "Word" },
						Confidences = new[] { 0.00347203249f, 0.477621228f },
						SourceSegmentRange = new RangeDto() { Start = 3, End = 5 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 },
							new AlignedWordPairDto { SourceIndex = 1, TargetIndex = 1 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 5,
						NextState = 7,
						Score = -8.49148f,
						Words = new[] { "the", "Word" },
						Confidences = new[] { 0.346071422f, 0.477621228f },
						SourceSegmentRange = new RangeDto() { Start = 3, End = 5 },
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
						NextState = 8,
						Score = -15.2926f,
						Words = new[] { "beginning" },
						Confidences = new[] { 0.348795831f },
						SourceSegmentRange = new RangeDto() { Start = 2, End = 3 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 2,
						NextState = 9,
						Score = -15.2926f,
						Words = new[] { "beginning" },
						Confidences = new[] { 0.348795831f },
						SourceSegmentRange = new RangeDto() { Start = 2, End = 3 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 7,
						NextState = 10,
						Score = -14.3453f,
						Words = new[] { "already" },
						Confidences = new[] { 0.2259867f },
						SourceSegmentRange = new RangeDto() { Start = 5, End = 6 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 8,
						NextState = 6,
						Score = -19.3042f,
						Words = new[] { "his", "Word" },
						Confidences = new[] { 0.00347203249f, 0.477621228f },
						SourceSegmentRange = new RangeDto() { Start = 3, End = 5 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 },
							new AlignedWordPairDto { SourceIndex = 1, TargetIndex = 1 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 8,
						NextState = 7,
						Score = -8.49148f,
						Words = new[] { "the", "Word" },
						Confidences = new[] { 0.346071422f, 0.477621228f },
						SourceSegmentRange = new RangeDto() { Start = 3, End = 5 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 },
							new AlignedWordPairDto { SourceIndex = 1, TargetIndex = 1 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 9,
						NextState = 6,
						Score = -19.3042f,
						Words = new[] { "his", "Word" },
						Confidences = new[] { 0.00347203249f, 0.477621228f },
						SourceSegmentRange = new RangeDto() { Start = 3, End = 5 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 },
							new AlignedWordPairDto { SourceIndex = 1, TargetIndex = 1 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 9,
						NextState = 7,
						Score = -8.49148f,
						Words = new[] { "the", "Word" },
						Confidences = new[] { 0.346071422f, 0.477621228f },
						SourceSegmentRange = new RangeDto() { Start = 3, End = 5 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 },
							new AlignedWordPairDto { SourceIndex = 1, TargetIndex = 1 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 6,
						NextState = 10,
						Score = -14.0526f,
						Words = new[] { "already" },
						Confidences = new[] { 0.2259867f },
						SourceSegmentRange = new RangeDto() { Start = 5, End = 6 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 10,
						NextState = 11,
						Score = 51.1117f,
						Words = new[] { "existía" },
						Confidences = new[] { 0.0f },
						SourceSegmentRange = new RangeDto() { Start = 6, End = 7 },
						IsUnknown = true,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 11,
						NextState = 12,
						Score = -29.0049f,
						Words = new[] { "you", "." },
						Confidences = new[] { 0.005803475f, 0.317073762f },
						SourceSegmentRange = new RangeDto() { Start = 7, End = 8 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 1 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 11,
						NextState = 13,
						Score = -27.7143f,
						Words = new[] { "to" },
						Confidences = new[] { 0.038961038f },
						SourceSegmentRange = new RangeDto() { Start = 7, End = 8 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 11,
						NextState = 14,
						Score = -30.0868f,
						Words = new[] { ".", "‘" },
						Confidences = new[] { 0.317073762f, 0.06190489f },
						SourceSegmentRange = new RangeDto() { Start = 7, End = 8 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 11,
						NextState = 15,
						Score = -30.1586f,
						Words = new[] { ".", "he" },
						Confidences = new[] { 0.317073762f, 0.06702433f },
						SourceSegmentRange = new RangeDto() { Start = 7, End = 8 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 11,
						NextState = 16,
						Score = -28.2444f,
						Words = new[] { ".", "the" },
						Confidences = new[] { 0.317073762f, 0.115540564f },
						SourceSegmentRange = new RangeDto() { Start = 7, End = 8 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 11,
						NextState = 17,
						Score = -23.8056f,
						Words = new[] { "and" },
						Confidences = new[] { 0.08047272f },
						SourceSegmentRange = new RangeDto() { Start = 7, End = 8 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 11,
						NextState = 18,
						Score = -23.5842f,
						Words = new[] { "the" },
						Confidences = new[] { 0.09361572f },
						SourceSegmentRange = new RangeDto() { Start = 7, End = 8 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 11,
						NextState = 19,
						Score = -18.8988f,
						Words = new[] { "," },
						Confidences = new[] { 0.1428188f },
						SourceSegmentRange = new RangeDto() { Start = 7, End = 8 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 11,
						NextState = 20,
						Score = -11.9218f,
						Words = new[] { ".", "’" },
						Confidences = new[] { 0.317073762f, 0.018057242f },
						SourceSegmentRange = new RangeDto() { Start = 7, End = 8 },
						IsUnknown = false,
						Alignment = new[]
						{
							new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 }
						}
					},
					new WordGraphArcDto
					{
						PrevState = 11,
						NextState = 21,
						Score = -3.51852f,
						Words = new[] { "." },
						Confidences = new[] { 0.317073762f },
						SourceSegmentRange = new RangeDto() { Start = 7, End = 8 },
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
				Target = new[] { "In", "el", "principio", "la", "Palabra", "ya", "existía", "." },
				Confidences = new[] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f },
				Sources = new[]
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
				Alignment = new[]
				{
					new AlignedWordPairDto { SourceIndex = 0, TargetIndex = 0 },
					new AlignedWordPairDto { SourceIndex = 1, TargetIndex = 1 },
					new AlignedWordPairDto { SourceIndex = 2, TargetIndex = 2 },
					new AlignedWordPairDto { SourceIndex = 3, TargetIndex = 3 },
					new AlignedWordPairDto { SourceIndex = 4, TargetIndex = 4 },
					new AlignedWordPairDto { SourceIndex = 5, TargetIndex = 5 },
					new AlignedWordPairDto { SourceIndex = 6, TargetIndex = 6 },
					new AlignedWordPairDto { SourceIndex = 7, TargetIndex = 7 }
				}
			}
		};

		private static MockHttpClient CreateWebClient()
		{
			var httpClient = new MockHttpClient();
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Post,
					Url = "translation/engines/project:project1/actions/interactiveTranslate",
					ResponseText = JsonConvert.SerializeObject(ResultDto, RestClientBase.SerializerSettings)
				});
			return httpClient;
		}

		[Ready]
		public static void RunTests()
		{
			QUnit.Module(nameof(InteractiveTranslationSessionTests));

			QUnit.Test(nameof(CurrentSuggestion_EmptyPrefix_ReturnsSuggestion),
				CurrentSuggestion_EmptyPrefix_ReturnsSuggestion);
			QUnit.Test(nameof(UpdatePrefix_AddOneCompleteWord_ReturnsSuggestion),
				UpdatePrefix_AddOneCompleteWord_ReturnsSuggestion);
			QUnit.Test(nameof(UpdatePrefix_AddOnePartialWord_ReturnsSuggestion),
				UpdatePrefix_AddOnePartialWord_ReturnsSuggestion);
			QUnit.Test(nameof(UpdatePrefix_RemoveOneWord_ReturnsSuggestion),
				UpdatePrefix_RemoveOneWord_ReturnsSuggestion);
			QUnit.Test(nameof(UpdatePrefix_RemoveEntirePrefix_ReturnsSuggestion),
				UpdatePrefix_RemoveEntirePrefix_ReturnsSuggestion);
			QUnit.Test(nameof(Approve_Success_ReturnsTrue), Approve_Success_ReturnsTrue);
			QUnit.Test(nameof(Approve_Error_ReturnsFalse), Approve_Error_ReturnsFalse);
			QUnit.Test(nameof(GetSuggestionTextInsertion_CompleteWord_ReturnsText),
				GetSuggestionTextInsertion_CompleteWord_ReturnsText);
			QUnit.Test(nameof(GetSuggestionTextInsertion_PartialWord_ReturnsText),
				GetSuggestionTextInsertion_PartialWord_ReturnsText);
			QUnit.Test(nameof(GetSuggestionTextInsertion_PartialWordFirstSuggestion_ReturnsText),
				GetSuggestionTextInsertion_PartialWordFirstSuggestion_ReturnsText);
			QUnit.Test(nameof(GetSuggestionTextInsertion_PartialWordSecondSuggestion_ReturnsText),
				GetSuggestionTextInsertion_PartialWordSecondSuggestion_ReturnsText);
		}

		private static void CurrentSuggestion_EmptyPrefix_ReturnsSuggestion(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost/", "project1", CreateWebClient());
			Action done = assert.Async();
			engine.TranslateInteractively("En el principio la Palabra ya existía.", 0.2, session =>
				{
					assert.NotEqual(session, null);

					assert.DeepEqual(session.CurrentSuggestion, "In the beginning the Word already".Split(" "));
					done();
				});
		}

		private static void UpdatePrefix_AddOneCompleteWord_ReturnsSuggestion(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost/", "project1", CreateWebClient());
			Action done = assert.Async();
			engine.TranslateInteractively("En el principio la Palabra ya existía.", 0.2, session =>
				{
					assert.NotEqual(session, null);

					assert.DeepEqual(session.UpdatePrefix("In "), "the beginning the Word already".Split(" "));
					done();
				});
		}

		private static void UpdatePrefix_AddOnePartialWord_ReturnsSuggestion(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost/", "project1", CreateWebClient());
			Action done = assert.Async();
			engine.TranslateInteractively("En el principio la Palabra ya existía.", 0.2, session =>
				{
					assert.NotEqual(session, null);
					session.UpdatePrefix("In ");

					assert.DeepEqual(session.UpdatePrefix("In t"), "the beginning the Word already".Split(" "));
					done();
				});
		}

		private static void UpdatePrefix_RemoveOneWord_ReturnsSuggestion(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost/", "project1", CreateWebClient());
			Action done = assert.Async();
			engine.TranslateInteractively("En el principio la Palabra ya existía.", 0.2, session =>
				{
					assert.NotEqual(session, null);
					session.UpdatePrefix("In the beginning ");

					assert.DeepEqual(session.UpdatePrefix("In the "), "beginning the Word already".Split(" "));
					done();
				});
		}

		private static void UpdatePrefix_RemoveEntirePrefix_ReturnsSuggestion(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost/", "project1", CreateWebClient());
			Action done = assert.Async();
			engine.TranslateInteractively("En el principio la Palabra ya existía.", 0.2, session =>
				{
					assert.NotEqual(session, null);
					session.UpdatePrefix("In the beginning ");

					assert.DeepEqual(session.UpdatePrefix(""), "In the beginning the Word already".Split(" "));
					done();
				});
		}

		private static void Approve_Success_ReturnsTrue(Assert assert)
		{
			string sourceSegment = "En el principio la Palabra ya existía.";
			string prefix = "In the beginning the Word already existed.";

			MockHttpClient httpClient = CreateWebClient();
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Post,
					Url = "translation/engines/project:project1/actions/trainSegment",
					Action = body =>
					{
						var segmentPair = JsonConvert.DeserializeObject<SegmentPairDto>(body,
							RestClientBase.SerializerSettings);
						var tokenizer = new LatinWordTokenizer();
						assert.DeepEqual(segmentPair.SourceSegment,
							tokenizer.TokenizeToStrings(sourceSegment).ToArray());
						assert.DeepEqual(segmentPair.TargetSegment, tokenizer.TokenizeToStrings(prefix).ToArray());
					},
					ResponseText = ""
				});

			var engine = new TranslationEngine("http://localhost/", "project1", httpClient);
			Action done = assert.Async();
			engine.TranslateInteractively(sourceSegment, 0.2, session =>
				{
					assert.NotEqual(session, null);
					session.UpdatePrefix(prefix);

					session.Approve(success =>
					{
						assert.Ok(success);
						done();
					});
				});
		}

		private static void Approve_Error_ReturnsFalse(Assert assert)
		{
			MockHttpClient httpClient = CreateWebClient();
			httpClient.Requests.Add(new MockRequest
				{
					Method = HttpRequestMethod.Post,
					Url = "translation/engines/project:project1/actions/trainSegment",
					ErrorStatus = 404
				});

			var engine = new TranslationEngine("http://localhost/", "project1", httpClient);
			Action done = assert.Async();
			engine.TranslateInteractively("En el principio la Palabra ya existía.", 0.2, session =>
				{
					assert.NotEqual(session, null);
					session.UpdatePrefix("In the beginning the Word already existed.");

					session.Approve(success =>
					{
						assert.NotOk(success);
						done();
					});
				});
		}

		private static void GetSuggestionTextInsertion_CompleteWord_ReturnsText(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost/", "project1", CreateWebClient());
			Action done = assert.Async();
			engine.TranslateInteractively("En el principio la Palabra ya existía.", 0.2, session =>
			{
				assert.NotEqual(session, null);

				session.UpdatePrefix("In ");
				TextInsertion change = session.GetSuggestionTextInsertion();
				assert.Equal(change.DeleteLength, 0);
				assert.Equal(change.InsertText, "the beginning the Word already");
				done();
			});
		}

		private static void GetSuggestionTextInsertion_PartialWord_ReturnsText(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost/", "project1", CreateWebClient());
			Action done = assert.Async();
			engine.TranslateInteractively("En el principio la Palabra ya existía.", 0.2, session =>
			{
				assert.NotEqual(session, null);
				session.UpdatePrefix("In ");

				session.UpdatePrefix("In t");
				TextInsertion change = session.GetSuggestionTextInsertion();
				assert.Equal(change.DeleteLength, 0);
				assert.Equal(change.InsertText, "he beginning the Word already");
				done();
			});
		}

		private static void GetSuggestionTextInsertion_PartialWordFirstSuggestion_ReturnsText(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost/", "project1", CreateWebClient());
			Action done = assert.Async();
			engine.TranslateInteractively("En el principio la Palabra ya existía.", 0.2, session =>
			{
				assert.NotEqual(session, null);
				session.UpdatePrefix("In ");

				session.UpdatePrefix("In t");
				TextInsertion change = session.GetSuggestionTextInsertion(0);
				assert.Equal(change.DeleteLength, 0);
				assert.Equal(change.InsertText, "he");
				done();
			});
		}

		private static void GetSuggestionTextInsertion_PartialWordSecondSuggestion_ReturnsText(Assert assert)
		{
			var engine = new TranslationEngine("http://localhost/", "project1", CreateWebClient());
			Action done = assert.Async();
			engine.TranslateInteractively("En el principio la Palabra ya existía.", 0.2, session =>
			{
				assert.NotEqual(session, null);
				session.UpdatePrefix("In ");

				session.UpdatePrefix("In t");
				TextInsertion change = session.GetSuggestionTextInsertion(1);
				assert.Equal(change.DeleteLength, 1);
				assert.Equal(change.InsertText, "beginning");
				done();
			});
		}
	}
}
