using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SIL.Machine.Translation;
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
		public async Task<IActionResult> StartSession(string sourceLanguageTag, string targetLanguageTag)
		{
			SessionContext sessionContext = await _engineService.TryCreateSession(sourceLanguageTag, targetLanguageTag);
			if (sessionContext != null)
				return new ObjectResult(sessionContext.CreateDto());
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/actions/translate")]
		public async Task<IActionResult> Translate(string sourceLanguageTag, string targetLanguageTag, [FromBody] string segment)
		{
			TranslationResult result = await _engineService.TryTranslate(sourceLanguageTag, targetLanguageTag, segment);
			if (result != null)
				return new ObjectResult(Enumerable.Range(0, result.TargetSegment.Count).Select(result.RecaseTargetWord).Detokenize());
			return NotFound();
		}
	}
}
