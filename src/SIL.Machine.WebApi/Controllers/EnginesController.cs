using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Services;

namespace SIL.Machine.WebApi.Controllers
{
	[Area("Translation")]
	[Route("[area]/[controller]")]
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
			return engines.Select(e => e.ToDto());
		}

		[HttpGet("{locatorType}:{locator}")]
		public async Task<IActionResult> GetAsync(string locatorType, string locator)
		{
			Engine engine = await _engineRepo.GetByLocatorAsync(GetLocatorType(locatorType), locator);
			if (engine == null)
				return NotFound();

			return Ok(engine.ToDto());
		}

		[HttpPost("{locatorType}:{locator}/actions/translate")]
		public async Task<IActionResult> TranslateAsync(string locatorType, string locator, [FromBody] string[] segment)
		{
			TranslationResult result = await _engineService.TranslateAsync(GetLocatorType(locatorType),
				locator, segment);
			if (result == null)
				return NotFound();
			return Ok(result.ToDto(segment));
		}

		[HttpPost("{locatorType}:{locator}/actions/translate/{n}")]
		public async Task<IActionResult> TranslateAsync(string locatorType, string locator, int n, [FromBody] string[] segment)
		{
			IEnumerable<TranslationResult> results = await _engineService.TranslateAsync(
				GetLocatorType(locatorType), locator, n, segment);
			if (results == null)
				return NotFound();
			return Ok(results.Select(tr => tr.ToDto(segment)));
		}

		[HttpPost("{locatorType}:{locator}/actions/interactiveTranslate")]
		public async Task<IActionResult> InteractiveTranslateAsync(string locatorType, string locator,
			[FromBody] string[] segment)
		{
			InteractiveTranslationResult result = await _engineService.InteractiveTranslateAsync(
				GetLocatorType(locatorType), locator, segment);
			if (result == null)
				return NotFound();
			return Ok(result.ToDto(segment));
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
	}
}
