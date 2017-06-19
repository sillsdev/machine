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
			return builds.Select(b => b.ToDto(Url));
		}

		[HttpGet("{locatorType}:{locator}")]
		public async Task<IActionResult> GetAsync(string locatorType, string locator)
		{
			Build build = await _buildRepo.GetByLocatorAsync(GetLocatorType(locatorType), locator);
			if (build == null)
				return NotFound();

			return Ok(build.ToDto(Url));
		}

		[HttpPost]
		public async Task<IActionResult> CreateAsync([FromBody] string engineId)
		{
			(Build Build, StartBuildStatus Status) result = await _engineService.StartBuildAsync(EngineLocatorType.Id, engineId);
			switch (result.Status)
			{
				case StartBuildStatus.EngineNotFound:
					return StatusCode(422);
				case StartBuildStatus.AlreadyBuilding:
					return StatusCode(409);
			}

			BuildDto dto = result.Build.ToDto(Url);
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
	}
}
