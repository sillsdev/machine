using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Controllers
{
	internal static class ControllersExtensions
	{
		public static ProjectDto ToProjectDto(this Engine engine, string projectId)
		{
			return new ProjectDto
			{
				Id = projectId,
				IsShared = engine.IsShared,
				SourceLanguageTag = engine.SourceLanguageTag,
				TargetLanguageTag = engine.TargetLanguageTag
			};
		}

		public static TranslationResultDto ToDto(this TranslationResult result, IReadOnlyList<string> sourceSegment)
		{
			return new TranslationResultDto
			{
				Target = Enumerable.Range(0, result.TargetSegment.Count)
					.Select(j => result.RecaseTargetWord(sourceSegment, j)).ToArray(),
				Confidences = result.TargetWordConfidences.Select(c => (float) c).ToArray(),
				Sources = result.TargetWordSources,
				Alignment = result.Alignment.ToDto()
			};
		}

		public static WordGraphDto ToDto(this WordGraph wordGraph, IReadOnlyList<string> sourceSegment)
		{
			return new WordGraphDto
			{
				InitialStateScore = (float) wordGraph.InitialStateScore,
				FinalStates = wordGraph.FinalStates.ToArray(),
				Arcs = wordGraph.Arcs.Select(a => a.ToDto(sourceSegment)).ToArray()
			};
		}

		public static WordGraphArcDto ToDto(this WordGraphArc arc, IReadOnlyList<string> sourceSegment)
		{
			return new WordGraphArcDto
			{
				PrevState = arc.PrevState,
				NextState = arc.NextState,
				Score = (float) arc.Score,
				Words = Enumerable.Range(0, arc.Words.Count)
					.Select(j => arc.Alignment.RecaseTargetWord(sourceSegment, arc.SourceStartIndex, arc.Words, j)).ToArray(),
				Confidences = arc.WordConfidences.Select(c => (float) c).ToArray(),
				SourceStartIndex = arc.SourceStartIndex,
				SourceEndIndex = arc.SourceEndIndex,
				IsUnknown = arc.IsUnknown,
				Alignment = arc.Alignment.ToDto()
			};
		}

		public static IReadOnlyList<AlignedWordPairDto> ToDto(this WordAlignmentMatrix matrix)
		{
			var wordPairs = new List<AlignedWordPairDto>();
			for (int i = 0; i < matrix.RowCount; i++)
			{
				for (int j = 0; j < matrix.ColumnCount; j++)
				{
					if (matrix[i, j] == AlignmentType.Aligned)
						wordPairs.Add(new AlignedWordPairDto {SourceIndex = i, TargetIndex = j});
				}
			}
			return wordPairs;
		}

		public static EngineDto ToDto(this Engine engine)
		{
			return new EngineDto
			{
				Id = engine.Id,
				SourceLanguageTag = engine.SourceLanguageTag,
				TargetLanguageTag = engine.TargetLanguageTag,
				IsShared = engine.IsShared,
				Projects = engine.Projects.ToArray()
			};
		}

		public static InteractiveTranslationResultDto ToDto(this InteractiveTranslationResult result,
			IReadOnlyList<string> sourceSegment)
		{
			return new InteractiveTranslationResultDto
			{
				WordGraph = result.SmtWordGraph.ToDto(sourceSegment),
				RuleResult = result.RuleResult?.ToDto(sourceSegment)
			};
		}

		public static BuildDto ToDto(this Build build)
		{
			return new BuildDto
			{
				Id = build.Id,
				Revision = build.Revision,
				Engine = build.EngineId,
				StepCount = build.StepCount,
				CurrentStep = build.CurrentStep,
				CurrentStepMessage = build.CurrentStepMessage
			};
		}
	}
}
