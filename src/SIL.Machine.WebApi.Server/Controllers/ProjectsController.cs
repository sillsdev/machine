using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SIL.Machine.WebApi.Dtos;
using SIL.Machine.WebApi.Server.DataAccess;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Services;

namespace SIL.Machine.WebApi.Server.Controllers
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
			return engines.SelectMany(e => e.Projects, CreateDto);
		}

		[HttpGet("id:{id}")]
		public async Task<IActionResult> GetAsync(string id)
		{
			Engine engine = await _engineRepo.GetByProjectIdAsync(id);
			if (engine == null)
				return NotFound();

			return Ok(CreateDto(engine, id));
		}

		[HttpPost]
		public async Task<IActionResult> CreateAsync([FromBody] ProjectDto newProject)
		{
			(Engine Engine, bool ProjectAdded) result = await _engineService.AddProjectAsync(newProject.SourceLanguageTag,
				newProject.TargetLanguageTag, newProject.Id, newProject.IsShared);
			if (!result.ProjectAdded)
				return StatusCode(409);

			ProjectDto dto = CreateDto(result.Engine, newProject.Id);
			return Created(dto.Href, dto);
		}

		[HttpDelete("id:{id}")]
		public async Task<IActionResult> DeleteAsync(string id)
		{
			if (!await _engineService.RemoveProjectAsync(id))
				return NotFound();
			return Ok();
		}

		private ProjectDto CreateDto(Engine engine, string projectId)
		{
			return new ProjectDto
			{
				Id = projectId,
				Href = Url.GetEntityUrl(RouteNames.Projects, projectId),
				IsShared = engine.IsShared,
				SourceLanguageTag = engine.SourceLanguageTag,
				TargetLanguageTag = engine.TargetLanguageTag,
				Engine = new LinkDto {Href = Url.GetEntityUrl(RouteNames.Engines, engine.Id)}
			};
		}
	}
}
