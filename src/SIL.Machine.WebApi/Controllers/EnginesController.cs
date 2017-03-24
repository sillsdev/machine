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
		public IEnumerable<LanguagePairDto> GetAllLanguagePairs()
		{
			return _engineService.GetAllLanguagePairs();
		}

		[HttpGet("{sourceLanguageTag}/{targetLanguageTag}")]
		public IActionResult GetLanguagePair(string sourceLanguageTag, string targetLanguageTag)
		{
			LanguagePairDto languagePair;
			if (_engineService.TryGetLanguagePair(sourceLanguageTag, targetLanguageTag, out languagePair))
				return new ObjectResult(languagePair);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/actions/translate")]
		public IActionResult Translate(string sourceLanguageTag, string targetLanguageTag, [FromBody] string[] segment)
		{
			TranslationResultDto result;
			if (_engineService.TryTranslate(sourceLanguageTag, targetLanguageTag, null, segment, out result))
				return new ObjectResult(result);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/actions/translate/{n}")]
		public IActionResult Translate(string sourceLanguageTag, string targetLanguageTag, int n, [FromBody] string[] segment)
		{
			IReadOnlyList<TranslationResultDto> results;
			if (_engineService.TryTranslate(sourceLanguageTag, targetLanguageTag, null, n, segment, out results))
				return new ObjectResult(results);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/actions/interactive-translate")]
		public IActionResult InteractiveTranslate(string sourceLanguageTag, string targetLanguageTag, [FromBody] string[] segment)
		{
			InteractiveTranslationResultDto result;
			if (_engineService.TryInteractiveTranslate(sourceLanguageTag, targetLanguageTag, null, segment, out result))
				return new ObjectResult(result);
			return NotFound();
		}

		[HttpGet("{sourceLanguageTag}/{targetLanguageTag}/projects")]
		public IActionResult GetAllProjects(string sourceLanguageTag, string targetLanguageTag)
		{
			IReadOnlyList<ProjectDto> projects;
			if (_engineService.GetAllProjects(sourceLanguageTag, targetLanguageTag, out projects))
				return new ObjectResult(projects);
			return NotFound();
		}

		[HttpGet("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}")]
		public IActionResult GetProject(string sourceLanguageTag, string targetLanguageTag, string projectId)
		{
			ProjectDto project;
			if (_engineService.TryGetProject(sourceLanguageTag, targetLanguageTag, projectId, out project))
				return new ObjectResult(project);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}/actions/translate")]
		public IActionResult Translate(string sourceLanguageTag, string targetLanguageTag, string projectId, [FromBody] string[] segment)
		{
			TranslationResultDto result;
			if (_engineService.TryTranslate(sourceLanguageTag, targetLanguageTag, projectId, segment, out result))
				return new ObjectResult(result);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}/actions/translate/{n}")]
		public IActionResult Translate(string sourceLanguageTag, string targetLanguageTag, string projectId, int n, [FromBody] string[] segment)
		{
			IReadOnlyList<TranslationResultDto> results;
			if (_engineService.TryTranslate(sourceLanguageTag, targetLanguageTag, projectId, n, segment, out results))
				return new ObjectResult(results);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}/actions/interactive-translate")]
		public IActionResult InteractiveTranslate(string sourceLanguageTag, string targetLanguageTag, string projectId, [FromBody] string[] segment)
		{
			InteractiveTranslationResultDto result;
			if (_engineService.TryInteractiveTranslate(sourceLanguageTag, targetLanguageTag, projectId, segment, out result))
				return new ObjectResult(result);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}/actions/train-segment")]
		public IActionResult TrainSegment(string sourceLanguageTag, string targetLanguageTag, string projectId, [FromBody] SegmentPairDto segmentPair)
		{
			if (_engineService.TryTrainSegment(sourceLanguageTag, targetLanguageTag, projectId, segmentPair))
				return Ok();
			return NotFound();
		}

		[InternalApi]
		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/projects")]
		public IActionResult AddProject(string sourceLanguageTag, string targetLanguageTag, [FromBody] ProjectDto newProject)
		{
			ProjectDto project = _engineService.AddProject(sourceLanguageTag, targetLanguageTag, newProject);
			return new ObjectResult(project);
		}

		[InternalApi]
		[HttpDelete("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}")]
		public IActionResult RemoveProject(string sourceLanguageTag, string targetLanguageTag, string projectId)
		{
			if (_engineService.RemoveProject(sourceLanguageTag, targetLanguageTag, projectId))
				return Ok();
			return NotFound();
		}
	}
}
