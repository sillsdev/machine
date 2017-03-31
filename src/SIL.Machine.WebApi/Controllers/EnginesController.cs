using System.Collections.Generic;
using System.Threading.Tasks;
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
		public async Task<IReadOnlyCollection<LanguagePairDto>> GetAllLanguagePairs()
		{
			return await _engineService.GetLanguagePairsAsync();
		}

		[HttpGet("{sourceLanguageTag}/{targetLanguageTag}")]
		public async Task<IActionResult> GetLanguagePair(string sourceLanguageTag, string targetLanguageTag)
		{
			LanguagePairDto result = await _engineService.GetLanguagePairAsync(sourceLanguageTag, targetLanguageTag);
			if (result != null)
				return new ObjectResult(result);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/actions/translate")]
		public async Task<IActionResult> Translate(string sourceLanguageTag, string targetLanguageTag, [FromBody] string[] segment)
		{
			TranslationResultDto result = await _engineService.TranslateAsync(sourceLanguageTag, targetLanguageTag, null, segment);
			if (result != null)
				return new ObjectResult(result);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/actions/translate/{n}")]
		public async Task<IActionResult> Translate(string sourceLanguageTag, string targetLanguageTag, int n, [FromBody] string[] segment)
		{
			IReadOnlyList<TranslationResultDto> results = await _engineService.TranslateAsync(sourceLanguageTag, targetLanguageTag, null, n, segment);
			if (results != null)
				return new ObjectResult(results);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/actions/interactive-translate")]
		public async Task<IActionResult> InteractiveTranslate(string sourceLanguageTag, string targetLanguageTag, [FromBody] string[] segment)
		{
			InteractiveTranslationResultDto result = await _engineService.InteractiveTranslateAsync(sourceLanguageTag, targetLanguageTag, null, segment);
			if (result != null)
				return new ObjectResult(result);
			return NotFound();
		}

		[HttpGet("{sourceLanguageTag}/{targetLanguageTag}/projects")]
		public async Task<IActionResult> GetAllProjects(string sourceLanguageTag, string targetLanguageTag)
		{
			IReadOnlyCollection<ProjectDto> results = await _engineService.GetProjectsAsync(sourceLanguageTag, targetLanguageTag);
			if (results != null)
				return new ObjectResult(results);
			return NotFound();
		}

		[HttpGet("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}")]
		public async Task<IActionResult> GetProject(string sourceLanguageTag, string targetLanguageTag, string projectId)
		{
			ProjectDto result = await _engineService.GetProjectAsync(sourceLanguageTag, targetLanguageTag, projectId);
			if (result != null)
				return new ObjectResult(result);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}/actions/translate")]
		public async Task<IActionResult> Translate(string sourceLanguageTag, string targetLanguageTag, string projectId, [FromBody] string[] segment)
		{
			TranslationResultDto result = await _engineService.TranslateAsync(sourceLanguageTag, targetLanguageTag, projectId, segment);
			if (result != null)
				return new ObjectResult(result);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}/actions/translate/{n}")]
		public async Task<IActionResult> Translate(string sourceLanguageTag, string targetLanguageTag, string projectId, int n, [FromBody] string[] segment)
		{
			IReadOnlyList<TranslationResultDto> results = await _engineService.TranslateAsync(sourceLanguageTag, targetLanguageTag, projectId, n, segment);
			if (results != null)
				return new ObjectResult(results);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}/actions/interactive-translate")]
		public async Task<IActionResult> InteractiveTranslate(string sourceLanguageTag, string targetLanguageTag, string projectId, [FromBody] string[] segment)
		{
			InteractiveTranslationResultDto result = await _engineService.InteractiveTranslateAsync(sourceLanguageTag, targetLanguageTag, projectId, segment);
			if (result != null)
				return new ObjectResult(result);
			return NotFound();
		}

		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}/actions/train-segment")]
		public async Task<IActionResult> TrainSegment(string sourceLanguageTag, string targetLanguageTag, string projectId, [FromBody] SegmentPairDto segmentPair)
		{
			if (await _engineService.TrainSegmentAsync(sourceLanguageTag, targetLanguageTag, projectId, segmentPair))
				return Ok();
			return NotFound();
		}

		[InternalApi]
		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/projects")]
		public async Task<IActionResult> AddProject(string sourceLanguageTag, string targetLanguageTag, [FromBody] ProjectDto newProject)
		{
			ProjectDto project = await _engineService.AddProjectAsync(sourceLanguageTag, targetLanguageTag, newProject);
			return new ObjectResult(project);
		}

		[InternalApi]
		[HttpDelete("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}")]
		public async Task<IActionResult> RemoveProject(string sourceLanguageTag, string targetLanguageTag, string projectId)
		{
			if (await _engineService.RemoveProjectAsync(sourceLanguageTag, targetLanguageTag, projectId))
				return Ok();
			return NotFound();
		}

		[InternalApi]
		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}/actions/start-rebuild")]
		public async Task<IActionResult> StartRebuild(string sourceLanguageTag, string targetLanguageTag, string projectId)
		{
			if (await _engineService.StartRebuildAsync(sourceLanguageTag, targetLanguageTag, projectId))
				return Ok();
			return NotFound();
		}

		[InternalApi]
		[HttpPost("{sourceLanguageTag}/{targetLanguageTag}/projects/{projectId}/actions/cancel-rebuild")]
		public async Task<IActionResult> CancelRebuild(string sourceLanguageTag, string targetLanguageTag, string projectId)
		{
			if (await _engineService.CancelRebuildAsync(sourceLanguageTag, targetLanguageTag, projectId))
				return Ok();
			return NotFound();
		}
	}
}
