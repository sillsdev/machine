using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Models
{
	internal static class ModelsExtensions
	{
		public static EngineDto CreateDto(this EngineContext engineContext)
		{
			return new EngineDto
			{
				SourceLanguageTag = engineContext.SourceLanguageTag,
				TargetLanguageTag = engineContext.TargetLanguageTag
			};
		}

		public static TranslationResultDto CreateDto(this TranslationResult result, IReadOnlyList<string> sourceSegment)
		{
			return new TranslationResultDto
			{
				Target = Enumerable.Range(0, result.TargetSegment.Count).Select(j => result.RecaseTargetWord(sourceSegment, j)).ToArray(),
				Confidences = result.TargetWordConfidences.Select(c => (float) c).ToArray(),
				Sources = result.TargetWordSources,
				Alignment = result.Alignment.CreateDto()
			};
		}

		public static WordGraphDto CreateDto(this WordGraph wordGraph, IReadOnlyList<string> sourceSegment)
		{
			return new WordGraphDto
			{
				InitialStateScore = (float) wordGraph.InitialStateScore,
				FinalStates = wordGraph.FinalStates.ToArray(),
				Arcs = wordGraph.Arcs.Select(a => a.CreateDto(sourceSegment)).ToArray()
			};
		}

		public static WordGraphArcDto CreateDto(this WordGraphArc arc, IReadOnlyList<string> sourceSegment)
		{
			return new WordGraphArcDto
			{
				PrevState = arc.PrevState,
				NextState = arc.NextState,
				Score = (float) arc.Score,
				Words = Enumerable.Range(0, arc.Words.Count).Select(j => arc.Alignment.RecaseTargetWord(sourceSegment, arc.SourceStartIndex, arc.Words, j)).ToArray(),
				Confidences = arc.WordConfidences.Select(c => (float) c).ToArray(),
				SourceStartIndex = arc.SourceStartIndex,
				SourceEndIndex = arc.SourceEndIndex,
				IsUnknown = arc.IsUnknown,
				Alignment = arc.Alignment.CreateDto()
			};
		}

		public static IReadOnlyList<AlignedWordPairDto> CreateDto(this WordAlignmentMatrix matrix)
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
	}
}
