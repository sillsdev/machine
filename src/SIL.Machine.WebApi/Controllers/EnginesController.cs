using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Services;
using SIL.Machine.Annotations;

namespace SIL.Machine.WebApi.Controllers
{
	[Area("Translation")]
	[Route("[area]/[controller]", Name = RouteNames.Engines)]
	public class EnginesController : Controller
	{
		private readonly IEngineRepository _engineRepo;
		private readonly EngineService _engineService;

		public EnginesController(IEngineRepository engineRepo, EngineService engineService)
		{
			_engineRepo = engineRepo;
			_engineService = engineService;
		}

		[HttpGet]
		public async Task<IEnumerable<EngineDto>> GetAllAsync()
		{
			IEnumerable<Engine> engines = await _engineRepo.GetAllAsync();
			return engines.Select(CreateDto);
		}

		[HttpGet("{locatorType}:{locator}")]
		public async Task<IActionResult> GetAsync(string locatorType, string locator)
		{
			Engine engine = await _engineRepo.GetByLocatorAsync(GetLocatorType(locatorType), locator);
			if (engine == null)
				return NotFound();

			return Ok(CreateDto(engine));
		}

		[HttpPost("{locatorType}:{locator}/actions/translate")]
		public async Task<IActionResult> TranslateAsync(string locatorType, string locator, [FromBody] string[] segment)
		{
			TranslationResult result = await _engineService.TranslateAsync(GetLocatorType(locatorType),
				locator, segment);
			if (result == null)
				return NotFound();
			return Ok(CreateDto(result, segment));
		}

		[HttpPost("{locatorType}:{locator}/actions/translate/{n}")]
		public async Task<IActionResult> TranslateAsync(string locatorType, string locator, int n,
			[FromBody] string[] segment)
		{
			IEnumerable<TranslationResult> results = await _engineService.TranslateAsync(
				GetLocatorType(locatorType), locator, n, segment);
			if (results == null)
				return NotFound();
			return Ok(results.Select(tr => CreateDto(tr, segment)));
		}

		[HttpPost("{locatorType}:{locator}/actions/interactiveTranslate")]
		public async Task<IActionResult> InteractiveTranslateAsync(string locatorType, string locator,
			[FromBody] string[] segment)
		{
			HybridInteractiveTranslationResult result = await _engineService.InteractiveTranslateAsync(
				GetLocatorType(locatorType), locator, segment);
			if (result == null)
				return NotFound();
			return Ok(CreateDto(result, segment));
		}

		[HttpPost("{locatorType}:{locator}/actions/trainSegment")]
		public async Task<IActionResult> TrainSegmentAsync(string locatorType, string locator,
			[FromBody] SegmentPairDto segmentPair)
		{
			if (!await _engineService.TrainSegmentAsync(GetLocatorType(locatorType), locator, segmentPair.SourceSegment,
				segmentPair.TargetSegment))
			{
				return NotFound();
			}
			return Ok();
		}

		private static EngineLocatorType GetLocatorType(string type)
		{
			switch (type)
			{
				case "id":
					return EngineLocatorType.Id;
				case "langTag":
					return EngineLocatorType.LanguageTag;
				case "project":
					return EngineLocatorType.Project;
			}
			return EngineLocatorType.Id;
		}

		private static TranslationResultDto CreateDto(TranslationResult result, IReadOnlyList<string> sourceSegment)
		{
			if (result == null)
				return null;

			return new TranslationResultDto
			{
				Target = Enumerable.Range(0, result.TargetSegment.Count)
					.Select(j => result.RecaseTargetWord(sourceSegment, j)).ToArray(),
				Confidences = result.WordConfidences.Select(c => (float) c).ToArray(),
				Sources = result.WordSources.ToArray(),
				Alignment = CreateDto(result.Alignment),
				Phrases = result.Phrases.Select(CreateDto).ToArray()
			};
		}

		private static WordGraphDto CreateDto(WordGraph wordGraph, IReadOnlyList<string> sourceSegment)
		{
			return new WordGraphDto
			{
				InitialStateScore = (float) wordGraph.InitialStateScore,
				FinalStates = wordGraph.FinalStates.ToArray(),
				Arcs = wordGraph.Arcs.Select(a => CreateDto(a, sourceSegment)).ToArray()
			};
		}

		private static WordGraphArcDto CreateDto(WordGraphArc arc, IReadOnlyList<string> sourceSegment)
		{
			return new WordGraphArcDto
			{
				PrevState = arc.PrevState,
				NextState = arc.NextState,
				Score = (float) arc.Score,
				Words = Enumerable.Range(0, arc.Words.Count)
					.Select(j =>
						arc.Alignment.RecaseTargetWord(sourceSegment, arc.SourceSegmentRange.Start, arc.Words, j))
					.ToArray(),
				Confidences = arc.WordConfidences.Select(c => (float) c).ToArray(),
				SourceSegmentRange = CreateDto(arc.SourceSegmentRange),
				IsUnknown = arc.IsUnknown,
				Alignment = CreateDto(arc.Alignment)
			};
		}

		private static AlignedWordPairDto[] CreateDto(WordAlignmentMatrix matrix)
		{
			var wordPairs = new List<AlignedWordPairDto>();
			for (int i = 0; i < matrix.RowCount; i++)
			{
				for (int j = 0; j < matrix.ColumnCount; j++)
				{
					if (matrix[i, j])
						wordPairs.Add(new AlignedWordPairDto { SourceIndex = i, TargetIndex = j });
				}
			}
			return wordPairs.ToArray();
		}

		private EngineDto CreateDto(Engine engine)
		{
			return new EngineDto
			{
				Id = engine.Id,
				Href = Url.GetEntityUrl(RouteNames.Engines, engine.Id),
				SourceLanguageTag = engine.SourceLanguageTag,
				TargetLanguageTag = engine.TargetLanguageTag,
				IsShared = engine.IsShared,
				Projects = engine.Projects.Select(projectId =>
					Url.CreateLinkDto(RouteNames.Projects, projectId)).ToArray(),
				Confidence = engine.Confidence
			};
		}

		private static RangeDto CreateDto(Range<int> range)
		{
			return new RangeDto()
			{
				Start = range.Start,
				End = range.End
			};
		}

		private static InteractiveTranslationResultDto CreateDto(HybridInteractiveTranslationResult result,
			IReadOnlyList<string> sourceSegment)
		{
			return new InteractiveTranslationResultDto
			{
				WordGraph = CreateDto(result.SmtWordGraph, sourceSegment),
				RuleResult = CreateDto(result.RuleResult, sourceSegment)
			};
		}

		private static PhraseDto CreateDto(Phrase phrase)
		{
			return new PhraseDto
			{
				SourceSegmentRange = CreateDto(phrase.SourceSegmentRange),
				TargetSegmentCut = phrase.TargetSegmentCut,
				Confidence = phrase.Confidence
			};
		}
	}
}
