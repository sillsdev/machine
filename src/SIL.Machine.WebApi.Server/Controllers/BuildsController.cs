using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
		public async Task<IActionResult> GetAsync(string locatorType, string locator, [FromQuery] long? minRevision,
			[FromQuery] bool? waitNew, CancellationToken ct)
		{
			Build build;
			if (minRevision != null || waitNew != null)
			{
				build = await _buildRepo.GetNewerRevisionAsync(GetLocatorType(locatorType), locator, minRevision ?? 0,
					waitNew ?? false, ct);
			}
			else
			{
				build = await _buildRepo.GetByLocatorAsync(GetLocatorType(locatorType), locator);
			}

			if (build == null)
				return NotFound();

			return Ok(CreateDto(build));
		}

		[HttpPost]
		public async Task<IActionResult> CreateAsync([FromBody] string engineId)
		{
			Build build = await _engineService.StartBuildAsync(EngineLocatorType.Id, engineId);
			if (build == null)
				return StatusCode(422);

			BuildDto dto = CreateDto(build);
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
				Engine = Url.CreateLinkDto(RouteNames.Engines, build.EngineId),
				StepCount = build.StepCount,
				CurrentStep = build.CurrentStep,
				CurrentStepMessage = build.CurrentStepMessage
			};
		}
	}
}
