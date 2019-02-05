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
		private readonly IRepository<Project> _projectRepo;
		private readonly IEngineService _engineService;

		public ProjectsController(IRepository<Project> projectRepo, IEngineService engineService)
		{
			_projectRepo = projectRepo;
			_engineService = engineService;
		}

		[HttpGet]
		public async Task<IEnumerable<ProjectDto>> GetAllAsync()
		{
			IEnumerable<Project> projects = await _projectRepo.GetAllAsync();
			return projects.Select(CreateDto);
		}

		[HttpGet("id:{id}")]
		public async Task<IActionResult> GetAsync(string id)
		{
			Project project = await _projectRepo.GetAsync(id);
			if (project == null)
				return NotFound();

			return Ok(CreateDto(project));
		}

		[HttpPost]
		public async Task<IActionResult> CreateAsync([FromBody] ProjectDto newProject)
		{
			Project project = await _engineService.AddProjectAsync(newProject.Id, newProject.SourceLanguageTag,
				newProject.TargetLanguageTag, newProject.SourceSegmentType, newProject.TargetSegmentType,
				newProject.IsShared);
			if (project == null)
				return StatusCode(409);

			ProjectDto dto = CreateDto(project);
			return Created(dto.Href, dto);
		}

		[HttpDelete("id:{id}")]
		public async Task<IActionResult> DeleteAsync(string id)
		{
			if (!await _engineService.RemoveProjectAsync(id))
				return NotFound();

			return Ok();
		}

		private ProjectDto CreateDto(Project project)
		{
			return new ProjectDto
			{
				Id = project.Id,
				Href = Url.GetEntityUrl(RouteNames.Projects, project.Id),
				SourceLanguageTag = project.SourceLanguageTag,
				TargetLanguageTag = project.TargetLanguageTag,
				SourceSegmentType = project.SourceSegmentType,
				TargetSegmentType = project.TargetSegmentType,
				IsShared = project.IsShared,
				Engine = Url.CreateLinkDto(RouteNames.Engines, project.EngineRef)
			};
		}
	}
}
