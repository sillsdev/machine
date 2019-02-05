using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SIL.Machine.WebApi.Configuration;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Services;
using SIL.Machine.WebApi.Utils;

namespace SIL.Machine.WebApi.Controllers
{
	[Area("Translation")]
	[Route("[area]/[controller]", Name = RouteNames.Builds)]
	public class BuildsController : Controller
	{
		private readonly IBuildRepository _buildRepo;
		private readonly IEngineService _engineService;
		private readonly IOptions<EngineOptions> _engineOptions;

		public BuildsController(IBuildRepository buildRepo, IEngineRepository engineRepo, IEngineService engineService,
			IOptions<EngineOptions> engineOptions)
		{
			_buildRepo = buildRepo;
			_engineService = engineService;
			_engineOptions = engineOptions;
		}

		[HttpGet]
		public async Task<IEnumerable<BuildDto>> GetAllAsync()
		{
			IEnumerable<Build> builds = await _buildRepo.GetAllAsync();
			return builds.Select(CreateDto);
		}

		[HttpGet("{locatorType}:{locator}")]
		public async Task<IActionResult> GetAsync(string locatorType, string locator, [FromQuery] long? minRevision,
			CancellationToken ct)
		{
			if (minRevision != null)
			{
				EntityChange<Build> change = await _buildRepo.GetNewerRevisionAsync(GetLocatorType(locatorType),
					locator, minRevision.Value, ct).Timeout(_engineOptions.Value.BuildLongPollTimeout, ct);
				switch (change.Type)
				{
					case EntityChangeType.None:
						return NoContent();
					case EntityChangeType.Delete:
						return NotFound();
					default:
						return Ok(CreateDto(change.Entity));
				}
			}

			Build build = await _buildRepo.GetByLocatorAsync(GetLocatorType(locatorType), locator);
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
				Engine = Url.CreateLinkDto(RouteNames.Engines, build.EngineRef),
				PercentCompleted = build.PercentCompleted,
				Message = build.Message,
				State = build.State
			};
		}
	}
}
