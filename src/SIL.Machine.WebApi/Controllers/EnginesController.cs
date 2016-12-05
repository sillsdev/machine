using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Controllers
{
	[Route("translation/[controller]")]
	public class EnginesController : Controller
	{
		private readonly IEngineService _engineService;

		public EnginesController(IEngineService engineService)
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
	}
}
