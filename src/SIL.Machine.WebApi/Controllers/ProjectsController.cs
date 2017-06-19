using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Services;

namespace SIL.Machine.WebApi.Controllers
{
	[Area("Translation")]
	[Route("[area]/[controller]", Name = RouteNames.Projects)]
	public class ProjectsController : Controller
	{
		private readonly IEngineRepository _engineRepo;
		private readonly EngineService _engineService;

		public ProjectsController(IEngineRepository engineRepo, EngineService engineService)
		{
			_engineRepo = engineRepo;
			_engineService = engineService;
		}

		[HttpGet]
		public async Task<IEnumerable<ProjectDto>> GetAllAsync()
		{
			IEnumerable<Engine> engines = await _engineRepo.GetAllAsync();
			return engines.SelectMany(e => e.Projects, (e, p) => e.ToProjectDto(p, Url));
		}

		[HttpGet("id:{id}")]
		public async Task<IActionResult> GetAsync(string id)
		{
			Engine engine = await _engineRepo.GetByProjectIdAsync(id);
			if (engine == null)
				return NotFound();

			return Ok(engine.ToProjectDto(id, Url));
		}

		[HttpPost]
		public async Task<IActionResult> CreateAsync([FromBody] ProjectDto newProject)
		{
			(Engine Engine, bool ProjectAdded) result = await _engineService.AddProjectAsync(newProject.SourceLanguageTag,
				newProject.TargetLanguageTag, newProject.Id, newProject.IsShared);
			if (!result.ProjectAdded)
				return StatusCode(409);

			ProjectDto dto = result.Engine.ToProjectDto(newProject.Id, Url);
			return Created(dto.Href, dto);
		}

		[HttpDelete("id:{id}")]
		public async Task<IActionResult> DeleteAsync(string id)
		{
			if (!await _engineService.RemoveProjectAsync(id))
				return NotFound();
			return Ok();
		}
	}
}
