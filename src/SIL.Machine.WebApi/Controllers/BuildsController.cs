using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
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
		private readonly IAuthorizationService _authService;
		private readonly IBuildRepository _builds;
		private readonly IEngineRepository _engines;
		private readonly IEngineService _engineService;
		private readonly IOptions<EngineOptions> _engineOptions;

		public BuildsController(IAuthorizationService authService, IBuildRepository builds,
			IEngineRepository engines, IEngineService engineService, IOptions<EngineOptions> engineOptions)
		{
			_authService = authService;
			_builds = builds;
			_engines = engines;
			_engineService = engineService;
			_engineOptions = engineOptions;
		}

		[HttpGet]
		public async Task<IEnumerable<BuildDto>> GetAllAsync()
		{
			var builds = new List<BuildDto>();
			foreach (Build build in await _builds.GetAllAsync())
			{
				Engine engine = await _engines.GetAsync(build.EngineRef);
				if (engine != null && await AuthorizeAsync(engine, Operations.Read))
					builds.Add(CreateDto(build));
			}
			return builds;
		}

		[HttpGet("{locatorType}:{locator}")]
		public async Task<IActionResult> GetAsync(string locatorType, string locator, [FromQuery] long? minRevision,
			CancellationToken ct)
		{
			BuildLocatorType buildLocatorType = GetLocatorType(locatorType);
			if (minRevision != null)
			{
				string engineId = null;
				switch (buildLocatorType)
				{
					case BuildLocatorType.Id:
						Build build = await _builds.GetAsync(locator);
						if (build == null)
							return NotFound();
						engineId = build.EngineRef;
						break;
					case BuildLocatorType.Engine:
						engineId = locator;
						break;
				}
				Engine engine = await _engines.GetAsync(engineId);
				if (engine == null)
					return NotFound();
				if (!await AuthorizeAsync(engine, Operations.Read))
					return StatusCode(StatusCodes.Status403Forbidden);

				EntityChange<Build> change = await _builds.GetNewerRevisionAsync(buildLocatorType, locator,
					minRevision.Value, ct).Timeout(_engineOptions.Value.BuildLongPollTimeout, ct);
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
			else
			{
				Build build = await _builds.GetByLocatorAsync(buildLocatorType, locator);
				if (build == null)
					return NotFound();
				Engine engine = await _engines.GetAsync(build.EngineRef);
				if (engine == null)
					return NotFound();
				if (!await AuthorizeAsync(engine, Operations.Read))
					return StatusCode(StatusCodes.Status403Forbidden);

				return Ok(CreateDto(build));
			}
		}

		[HttpPost]
		public async Task<IActionResult> CreateAsync([FromBody] string engineId)
		{
			Engine engine = await _engines.GetAsync(engineId);
			if (engine == null)
				return StatusCode(StatusCodes.Status422UnprocessableEntity);
			if (!await AuthorizeAsync(engine, Operations.Update))
				return StatusCode(StatusCodes.Status403Forbidden);

			Build build = await _engineService.StartBuildAsync(engine.Id);
			if (build == null)
				return StatusCode(StatusCodes.Status422UnprocessableEntity);
			BuildDto dto = CreateDto(build);
			return Created(dto.Href, dto);
		}

		[HttpDelete("{locatorType}:{locator}")]
		public async Task<IActionResult> DeleteAsync(string locatorType, string locator)
		{
			Build build = await _builds.GetByLocatorAsync(GetLocatorType(locatorType), locator);
			if (build == null)
				return NotFound();
			Engine engine = await _engines.GetAsync(build.EngineRef);
			if (engine == null)
				return NotFound();
			if (!await AuthorizeAsync(engine, Operations.Update))
				return StatusCode(StatusCodes.Status403Forbidden);

			await _engineService.CancelBuildAsync(engine.Id);
			return Ok();
		}

		private async Task<bool> AuthorizeAsync(Engine engine, OperationAuthorizationRequirement operation)
		{
			AuthorizationResult result = await _authService.AuthorizeAsync(User, engine, operation);
			return result.Succeeded;
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
