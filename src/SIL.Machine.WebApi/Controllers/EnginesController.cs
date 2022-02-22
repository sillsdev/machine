namespace SIL.Machine.WebApi.Controllers;

/// <summary>
/// Machine translation engines
/// </summary>
[Area("Translation")]
[Route("[area]/[controller]", Name = RouteNames.Engines)]
[Produces("application/json")]
public class EnginesController : Controller
{
	private readonly IAuthorizationService _authService;
	private readonly IRepository<Engine> _engines;
	private readonly IBuildRepository _builds;
	private readonly IEngineService _engineService;
	private readonly IOptions<EngineOptions> _engineOptions;

	public EnginesController(IAuthorizationService authService, IRepository<Engine> engines, IBuildRepository builds,
		IEngineService engineService, IOptions<EngineOptions> engineOptions)
	{
		_authService = authService;
		_engines = engines;
		_builds = builds;
		_engineService = engineService;
		_engineOptions = engineOptions;
	}

	[HttpGet]
	public async Task<IEnumerable<EngineDto>> GetAllAsync()
	{
		var engines = new List<EngineDto>();
		foreach (Engine engine in await _engines.GetAllAsync())
		{
			if (await AuthorizeAsync(engine, Operations.Read))
				engines.Add(CreateDto(engine));
		}
		return engines;
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<EngineDto>> GetAsync(string id)
	{
		Engine engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeAsync(engine, Operations.Read))
			return Forbid();

		return Ok(CreateDto(engine));
	}

	[HttpPost("{id}/translate")]
	public async Task<ActionResult<TranslationResultDto>> TranslateAsync(string id, [FromBody] string[] segment)
	{
		Engine engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeAsync(engine, Operations.Read))
			return Forbid();

		TranslationResult result = await _engineService.TranslateAsync(engine.Id, segment);
		if (result == null)
			return NotFound();
		return Ok(CreateDto(result));
	}

	[HttpPost("{id}/translate/{n}")]
	public async Task<ActionResult<IEnumerable<TranslationResultDto>>> TranslateAsync(string id, int n,
		[FromBody] string[] segment)
	{
		Engine engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeAsync(engine, Operations.Read))
			return Forbid();

		IEnumerable<TranslationResult> results = await _engineService.TranslateAsync(engine.Id, n, segment);
		if (results == null)
			return NotFound();
		return Ok(results.Select(CreateDto));
	}

	[HttpPost("{id}/get-word-graph")]
	public async Task<ActionResult<WordGraphDto>> InteractiveTranslateAsync(string id, [FromBody] string[] segment)
	{
		Engine engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeAsync(engine, Operations.Read))
			return Forbid();

		WordGraph result = await _engineService.GetWordGraphAsync(engine.Id, segment);
		if (result == null)
			return NotFound();
		return Ok(CreateDto(result));
	}

	[HttpPost("{id}/train-segment")]
	[ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
	public async Task<ActionResult> TrainSegmentAsync(string id, [FromBody] SegmentPairDto segmentPair)
	{
		Engine engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeAsync(engine, Operations.Update))
			return Forbid();

		if (!await _engineService.TrainSegmentAsync(engine.Id, segmentPair.SourceSegment,
			segmentPair.TargetSegment, segmentPair.SentenceStart))
		{
			return NotFound();
		}
		return Ok();
	}

	/// <summary>
	/// Gets the currently running build job.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <param name="minRevision">The minimum revision.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <response code="200">The build job.</response>
	[HttpGet("{id}/current-build")]
	public async Task<ActionResult<BuildDto>> GetCurrentBuildAsync(string id, [FromQuery] long? minRevision,
		CancellationToken ct)
	{
		Engine engine = await _engines.GetAsync(id, ct);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeAsync(engine, Operations.Read))
			return Forbid();

		if (minRevision != null)
		{
			EntityChange<Build> change = await _builds.GetNewerRevisionByEngineIdAsync(id,
				minRevision.Value, ct).Timeout(_engineOptions.Value.BuildLongPollTimeout, ct);
			return change.Type switch
			{
				EntityChangeType.None => StatusCode(StatusCodes.Status408RequestTimeout),
				EntityChangeType.Delete => NoContent(),
				_ => Ok(CreateDto(change.Entity)),
			};
		}
		else
		{
			Build build = await _builds.GetByEngineIdAsync(id, ct);
			if (build == null)
				return NoContent();

			return Ok(CreateDto(build));
		}
	}

	/// <summary>
	/// Gets all build jobs.
	/// </summary>
	/// <response code="200">The build jobs.</response>
	[HttpGet("{id}/builds")]
	public async Task<ActionResult<IEnumerable<BuildDto>>> GetAllBuildsAsync(string id)
	{
		Engine engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeAsync(engine, Operations.Read))
			return Forbid();

		return Ok((await _builds.GetAllAsync()).Select(CreateDto));
	}

	/// <summary>
	/// Gets the specified build job.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <param name="buildId">The build job id.</param>
	/// <param name="minRevision">The minimum revision.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <response code="200">The build job.</response>
	[HttpGet("{id}/builds/{buildId}")]
	public async Task<ActionResult<BuildDto>> GetBuildAsync(string id, string buildId, [FromQuery] long? minRevision,
		CancellationToken ct)
	{
		Engine engine = await _engines.GetAsync(id, ct);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeAsync(engine, Operations.Read))
			return Forbid();

		if (minRevision != null)
		{
			EntityChange<Build> change = await _builds.GetNewerRevisionAsync(buildId,
				minRevision.Value, ct).Timeout(_engineOptions.Value.BuildLongPollTimeout, ct);
			return change.Type switch
			{
				EntityChangeType.None => StatusCode(StatusCodes.Status408RequestTimeout),
				EntityChangeType.Delete => NotFound(),
				_ => Ok(CreateDto(change.Entity)),
			};
		}
		else
		{
			Build build = await _builds.GetAsync(buildId, ct);
			if (build == null)
				return NotFound();

			return Ok(CreateDto(build));
		}
	}

	/// <summary>
	/// Starts a build job for the specified engine.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <response code="201">The build job was started successfully.</response>
	[HttpPost("{id}/builds")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<BuildDto>> CreateBuildAsync(string id)
	{
		Engine engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeAsync(engine, Operations.Update))
			return Forbid();

		Build build = await _engineService.StartBuildAsync(id);
		if (build == null)
			return UnprocessableEntity();
		BuildDto dto = CreateDto(build);
		return Created(dto.Href, dto);
	}

	/// <summary>
	/// Cancels a build job.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <param name="buildId">The build job id.</param>
	/// <response code="200">The build job was cancelled successfully.</response>
	[HttpPost("{id}/builds/{buildId}/cancel")]
	[ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
	public async Task<ActionResult> CancelBuildAsync(string id, string buildId)
	{
		Engine engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeAsync(engine, Operations.Update))
			return Forbid();

		Build build = await _builds.GetAsync(buildId);
		if (build == null)
			return NotFound();

		await _engineService.CancelBuildAsync(id);
		return Ok();
	}

	private async Task<bool> AuthorizeAsync(Engine engine, OperationAuthorizationRequirement operation)
	{
		AuthorizationResult result = await _authService.AuthorizeAsync(User, engine, operation);
		return result.Succeeded;
	}

	private static TranslationResultDto CreateDto(TranslationResult result)
	{
		if (result == null)
			return null;

		return new TranslationResultDto
		{
			Target = result.TargetSegment.ToArray(),
			Confidences = result.WordConfidences.Select(c => (float)c).ToArray(),
			Sources = result.WordSources.ToArray(),
			Alignment = CreateDto(result.Alignment),
			Phrases = result.Phrases.Select(CreateDto).ToArray()
		};
	}

	private static WordGraphDto CreateDto(WordGraph wordGraph)
	{
		return new WordGraphDto
		{
			InitialStateScore = (float)wordGraph.InitialStateScore,
			FinalStates = wordGraph.FinalStates.ToArray(),
			Arcs = wordGraph.Arcs.Select(CreateDto).ToArray()
		};
	}

	private static WordGraphArcDto CreateDto(WordGraphArc arc)
	{
		return new WordGraphArcDto
		{
			PrevState = arc.PrevState,
			NextState = arc.NextState,
			Score = (float)arc.Score,
			Words = arc.Words.ToArray(),
			Confidences = arc.WordConfidences.Select(c => (float)c).ToArray(),
			SourceSegmentRange = CreateDto(arc.SourceSegmentRange),
			Sources = arc.WordSources.ToArray(),
			Alignment = CreateDto(arc.Alignment)
		};
	}

	private static AlignedWordPairDto[] CreateDto(WordAlignmentMatrix matrix)
	{
		var wordPairs = new List<AlignedWordPairDto>();
		for (int i = 0; i < matrix.RowCount; i++)
		{
			for (int j = 0; j < matrix.ColumnCount; j++)
			{
				if (matrix[i, j])
					wordPairs.Add(new AlignedWordPairDto { SourceIndex = i, TargetIndex = j });
			}
		}
		return wordPairs.ToArray();
	}

	private EngineDto CreateDto(Engine engine)
	{
		return new EngineDto
		{
			Id = engine.Id,
			Href = Url.RouteUrl(RouteNames.Engines) + $"/{engine.Id}",
			SourceLanguageTag = engine.SourceLanguageTag,
			TargetLanguageTag = engine.TargetLanguageTag,
			Confidence = engine.Confidence,
			TrainedSegmentCount = engine.TrainedSegmentCount
		};
	}

	private static RangeDto CreateDto(Range<int> range)
	{
		return new RangeDto()
		{
			Start = range.Start,
			End = range.End
		};
	}

	private static PhraseDto CreateDto(Phrase phrase)
	{
		return new PhraseDto
		{
			SourceSegmentRange = CreateDto(phrase.SourceSegmentRange),
			TargetSegmentCut = phrase.TargetSegmentCut,
			Confidence = phrase.Confidence
		};
	}

	private string GetEntityUrl(string routeName, string relativeUrl)
	{
		return Url.RouteUrl(routeName) + $"/{relativeUrl}";
	}

	private BuildDto CreateDto(Build build)
	{
		return new BuildDto
		{
			Id = build.Id,
			Href = Url.RouteUrl(RouteNames.Engines) + $"/builds/{build.Id}",
			Revision = build.Revision,
			Engine = new ResourceDto
			{
				Id = build.EngineRef,
				Href = Url.RouteUrl(RouteNames.Engines) + $"/{build.EngineRef}"
			},
			PercentCompleted = build.PercentCompleted,
			Message = build.Message,
			State = build.State
		};
	}
}
