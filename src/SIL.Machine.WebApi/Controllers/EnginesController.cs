using System.Collections.Generic;
using System.Linq;
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
			return _engineService.GetAll().Select(e => e.CreateDto());
		}

		[HttpGet("{sourceLanguageTag}/{targetLanguageTag}")]
		public IActionResult Get(string sourceLanguageTag, string targetLanguageTag)
		{
			EngineContext engineContext;
			if (_engineService.TryGet(sourceLanguageTag, targetLanguageTag, out engineContext))
				return new ObjectResult(engineContext.CreateDto());
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/actions/start-session")]
		public IActionResult StartSession(string sourceLanguageTag, string targetLanguageTag)
		{
			SessionContext sessionContext;
			if (_engineService.TryCreateSession(sourceLanguageTag, targetLanguageTag, out sessionContext))
				return new ObjectResult(sessionContext.CreateDto());
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
	}
}
