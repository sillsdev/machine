namespace SIL.Machine.WebApi.Controllers;

/// <summary>
/// Machine translation engine build jobs
/// </summary>
[Area("Translation")]
[Route("[area]/[controller]", Name = RouteNames.Builds)]
[Produces("application/json")]
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

	/// <summary>
	/// Gets all build jobs.
	/// </summary>
	/// <response code="200">The build jobs.</response>
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

	/// <summary>
	/// Gets the specified build job.
	/// </summary>
	/// <param name="locatorType">
	/// The locator type:
	/// - id: build id
	/// - engine: engine id
	/// </param>
	/// <param name="locator">The locator.</param>
	/// <param name="minRevision">The minimum revision.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <response code="200">The build job.</response>
	[HttpGet("{locatorType}:{locator}")]
	public async Task<ActionResult<BuildDto>> GetAsync(string locatorType, string locator,
		[FromQuery] long? minRevision, CancellationToken ct)
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
				return Forbid();

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
				return Forbid();

			return Ok(CreateDto(build));
		}
	}

	/// <summary>
	/// Starts a build job for the specified engine.
	/// </summary>
	/// <param name="engineId">The engine id.</param>
	/// <response code="201">The build job was started successfully.</response>
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<BuildDto>> CreateAsync([FromBody] string engineId)
	{
		Engine engine = await _engines.GetAsync(engineId);
		if (engine == null)
			return UnprocessableEntity();
		if (!await AuthorizeAsync(engine, Operations.Update))
			return Forbid();

		Build build = await _engineService.StartBuildAsync(engine.Id);
		if (build == null)
			return UnprocessableEntity();
		BuildDto dto = CreateDto(build);
		return Created(dto.Href, dto);
	}

	/// <summary>
	/// Cancels a build job.
	/// </summary>
	/// <param name="locatorType">
	/// The locator type:
	/// - id: build id
	/// - engine: engine id
	/// </param>
	/// <param name="locator">The locator.</param>
	/// <response code="200">The build job was cancelled successfully.</response>
	[HttpDelete("{locatorType}:{locator}")]
	[ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
	public async Task<ActionResult> DeleteAsync(string locatorType, string locator)
	{
		Build build = await _builds.GetByLocatorAsync(GetLocatorType(locatorType), locator);
		if (build == null)
			return NotFound();
		Engine engine = await _engines.GetAsync(build.EngineRef);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeAsync(engine, Operations.Update))
			return Forbid();

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
