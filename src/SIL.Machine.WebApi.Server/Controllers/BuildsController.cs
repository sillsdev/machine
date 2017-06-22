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
	[Route("[area]/[controller]", Name = RouteNames.Builds)]
	public class BuildsController : Controller
	{
		private readonly IBuildRepository _buildRepo;
		private readonly EngineService _engineService;

		public BuildsController(IBuildRepository buildRepo, IEngineRepository engineRepo, EngineService engineService)
		{
			_buildRepo = buildRepo;
			_engineService = engineService;
		}

		[HttpGet]
		public async Task<IEnumerable<BuildDto>> GetAllAsync()
		{
			IEnumerable<Build> builds = await _buildRepo.GetAllAsync();
			return builds.Select(CreateDto);
		}

		[HttpGet("{locatorType}:{locator}")]
		public async Task<IActionResult> GetAsync(string locatorType, string locator, [FromQuery] long? minRevision)
		{
			Build build = await _buildRepo.GetByLocatorAsync(GetLocatorType(locatorType), locator);
			if (build == null)
				return NotFound();

			if (minRevision != null && minRevision > build.Revision)
			{
				build = await _buildRepo.GetNewerRevisionAsync(build.Id, (long) minRevision);
				if (build == null)
					return NotFound();
			}

			return Ok(CreateDto(build));
		}

		[HttpPost]
		public async Task<IActionResult> CreateAsync([FromBody] string engineId)
		{
			(Build Build, StartBuildStatus Status) result = await _engineService.StartBuildAsync(EngineLocatorType.Id, engineId);
			if (result.Status == StartBuildStatus.EngineNotFound)
				return StatusCode(422);

			BuildDto dto = CreateDto(result.Build);
			if (result.Status == StartBuildStatus.AlreadyBuilding)
				return Ok(dto);

			return Created(dto.Href, dto);
		}

		[HttpDelete("{locatorType}:{locator}")]
		public async Task<IActionResult> DeleteAsync(string locatorType, string locator)
		{
			if (!await _engineService.CancelBuildAsync(GetLocatorType(locatorType), locator))
				return NotFound();
			return Ok();
		}

		private static BuildLocatorType GetLocatorType(string type)
		{
			switch (type)
			{
				case "id":
					return BuildLocatorType.Id;
				case "engine":
					return BuildLocatorType.Engine;
			}
			return BuildLocatorType.Id;
		}

		private BuildDto CreateDto(Build build)
		{
			return new BuildDto
			{
				Id = build.Id,
				Href = Url.GetEntityUrl(RouteNames.Builds, build.Id),
				Revision = build.Revision,
				Engine = new LinkDto {Href = Url.GetEntityUrl(RouteNames.Engines, build.EngineId)},
				StepCount = build.StepCount,
				CurrentStep = build.CurrentStep,
				CurrentStepMessage = build.CurrentStepMessage
			};
		}
	}
}
