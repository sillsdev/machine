using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SIL.Machine.WebApi.Filters;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Services;

namespace SIL.Machine.WebApi.Controllers
{
	[Route("translation/[controller]")]
	public class EnginesController : Controller
	{
		private readonly EngineService _engineService;

		public EnginesController(EngineService engineService)
		{
			_engineService = engineService;
		}

		[HttpGet]
		public IEnumerable<EngineDto> GetAll()
		{
			return _engineService.GetAll();
		}

		[HttpGet("{sourceLanguageTag}/{targetLanguageTag}")]
		public IActionResult Get(string sourceLanguageTag, string targetLanguageTag)
		{
			EngineDto engine;
			if (_engineService.TryGet(sourceLanguageTag, targetLanguageTag, out engine))
				return new ObjectResult(engine);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/actions/translate")]
		public IActionResult Translate(string sourceLanguageTag, string targetLanguageTag, [FromBody] string segment)
		{
			string result;
			if (_engineService.TryTranslate(sourceLanguageTag, targetLanguageTag, segment, out result))
				return new ObjectResult(result);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/actions/interactive-translate")]
		public IActionResult InteractiveTranslate(string sourceLanguageTag, string targetLanguageTag, [FromBody] string[] segment)
		{
			InteractiveTranslationResultDto result;
			if (_engineService.TryInteractiveTranslate(sourceLanguageTag, targetLanguageTag, segment, out result))
				return new ObjectResult(result);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/actions/train-segment")]
		public IActionResult TrainSegment(string sourceLanguageTag, string targetLanguageTag, [FromBody] SegmentPairDto segmentPair)
		{
			if (_engineService.TryTrainSegment(sourceLanguageTag, targetLanguageTag, segmentPair))
				return Ok();
			return NotFound();
		}

		[InternalApi]
		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}")]
		public IActionResult Add(string sourceLanguageTag, string targetLanguageTag)
		{
			EngineDto engine;
			if (_engineService.Add(sourceLanguageTag, targetLanguageTag, out engine))
				return new ObjectResult(engine);
			return NotFound();
		}

		[InternalApi]
		[HttpDelete("{sourceLanguageTag}/{targetLanguageTag}")]
		public IActionResult Remove(string sourceLanguageTag, string targetLanguageTag)
		{
			if (_engineService.Remove(sourceLanguageTag, targetLanguageTag))
				return Ok();
			return NotFound();
		}
	}
}
