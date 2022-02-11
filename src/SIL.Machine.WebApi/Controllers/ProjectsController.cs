using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Services;

namespace SIL.Machine.WebApi.Controllers
{
	/// <summary>
	/// Machine translation projects
	/// </summary>
	[Area("Translation")]
	[Route("[area]/[controller]", Name = RouteNames.Projects)]
	[Produces("application/json")]
	public class ProjectsController : Controller
	{
		private readonly IAuthorizationService _authService;
		private readonly IProjectRepository _projects;
		private readonly IEngineService _engineService;

		public ProjectsController(IAuthorizationService authService, IProjectRepository projects,
			IEngineService engineService)
		{
			_authService = authService;
			_projects = projects;
			_engineService = engineService;
		}

		[HttpGet]
		public async Task<IEnumerable<ProjectDto>> GetAllAsync()
		{
			var projects = new List<ProjectDto>();
			foreach (Project project in await _projects.GetAllAsync())
			{
				if (await AuthorizeAsync(project, Operations.Read))
					projects.Add(CreateDto(project));
			}
			return projects;
		}

		[HttpGet("id:{id}")]
		public async Task<ActionResult<ProjectDto>> GetAsync(string id)
		{
			Project project = await _projects.GetAsync(id);
			if (project == null)
				return NotFound();
			if (!await AuthorizeAsync(project, Operations.Read))
				return Forbid();

			return Ok(CreateDto(project));
		}

		[HttpPost]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<ProjectDto>> CreateAsync([FromBody] ProjectDto newProject)
		{
			var project = new Project
			{
				Id = newProject.Id,
				SourceLanguageTag = newProject.SourceLanguageTag,
				TargetLanguageTag = newProject.TargetLanguageTag,
				SourceSegmentType = newProject.SourceSegmentType,
				TargetSegmentType = newProject.TargetSegmentType,
				IsShared = newProject.IsShared
			};
			if (!await AuthorizeAsync(project, Operations.Create))
				return Forbid();
			bool created = await _engineService.AddProjectAsync(project);
			if (!created)
				return Conflict();

			ProjectDto dto = CreateDto(project);
			return Created(dto.Href, dto);
		}

		[HttpDelete("id:{id}")]
		[ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
		public async Task<ActionResult> DeleteAsync(string id)
		{
			Project project = await _projects.GetAsync(id);
			if (project == null)
				return NotFound();
			if (!await AuthorizeAsync(project, Operations.Read))
				return Forbid();

			if (!await _engineService.RemoveProjectAsync(project.Id))
				return NotFound();

			return Ok();
		}

		private async Task<bool> AuthorizeAsync(Project project, OperationAuthorizationRequirement operation)
		{
			AuthorizationResult result = await _authService.AuthorizeAsync(User, project, operation);
			return result.Succeeded;
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
