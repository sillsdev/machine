namespace SIL.Machine.WebApi.Controllers;

[Route("corpora")]
public class CorporaController : ControllerBase
{
	private readonly ICorpusService _corpusService;
	private readonly IMapper _mapper;

	public CorporaController(IAuthorizationService authService, ICorpusService corpusService, IMapper mapper)
		: base(authService)
	{
		_corpusService = corpusService;
		_mapper = mapper;
	}

	/// <summary>
	/// Gets all corpora.
	/// </summary>
	/// <response code="200">The corpora.</response>
	[Authorize(Scopes.ReadCorpora)]
	[HttpGet]
	public async Task<IEnumerable<CorpusDto>> GetAllAsync()
	{
		return (await _corpusService.GetAllAsync(User.Identity!.Name!)).Select(_mapper.Map<CorpusDto>);
	}

	/// <summary>
	/// Gets a corpus.
	/// </summary>
	/// <param name="id">The corpus id.</param>
	/// <response code="200">The corpus.</response>
	[Authorize(Scopes.ReadCorpora)]
	[HttpGet("{id}")]
	public async Task<ActionResult<CorpusDto>> GetAsync(string id)
	{
		Corpus? corpus = await _corpusService.GetAsync(id);
		if (corpus == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(corpus))
			return Forbid();

		return Ok(_mapper.Map<CorpusDto>(corpus));
	}

	/// <summary>
	/// Creates a new corpus.
	/// </summary>
	/// <param name="corpusConfig">The corpus configuration.</param>
	/// <response code="201">The corpus was created successfully.</response>
	[Authorize(Scopes.CreateCorpora)]
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<CorpusDto>> CreateAsync([FromBody] NewCorpusDto corpusConfig)
	{
		var newCorpus = new Corpus
		{
			Name = corpusConfig.Name,
			Type = corpusConfig.Type,
			Format = corpusConfig.Format
		};

		await _corpusService.CreateAsync(newCorpus);
		var dto = _mapper.Map<CorpusDto>(newCorpus);
		return Created(dto.Href, dto);
	}

	/// <summary>
	/// Deletes a corpus.
	/// </summary>
	/// <param name="id">The corpus id.</param>
	/// <response code="200">The corpus was successfully deleted.</response>
	[Authorize(Scopes.DeleteCorpora)]
	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteAsync(string id)
	{
		Corpus? corpus = await _corpusService.GetAsync(id);
		if (corpus == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(corpus))
			return Forbid();

		if (!await _corpusService.DeleteAsync(id))
			return NotFound();
		return Ok();
	}

	/// <summary>
	/// Uploads a data file to a corpus.
	/// </summary>
	/// <param name="id">The corpus id.</param>
	/// <param name="languageTag">The language tag.</param>
	/// <param name="textId">The text id.</param>
	/// <param name="file">The data file.</param>
	/// <response code="201">The data file was uploaded successfully.</response>
	[Authorize(Scopes.UpdateCorpora)]
	[HttpPost("{id}/files")]
	[RequestSizeLimit(100_000_000)]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<DataFileDto>> UploadDataFileAsync(string id,
		[BindRequired][FromForm] string languageTag, [FromForm] string? textId, [BindRequired] IFormFile file)
	{
		Corpus? corpus = await _corpusService.GetAsync(id);
		if (corpus == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(corpus))
			return Forbid();

		var dataFile = new DataFile
		{
			Id = ObjectId.GenerateNewId().ToString(),
			Name = file.FileName,
			LanguageTag = languageTag,
			TextId = textId
		};
		using (Stream stream = file.OpenReadStream())
		{
			await _corpusService.AddDataFileAsync(id, dataFile, stream);
		}
		var dto = Map(id, dataFile);
		return Created(dto.Href, dto);
	}

	/// <summary>
	/// Gets all files for a corpus.
	/// </summary>
	/// <param name="id">The corpus id.</param>
	/// <response code="200">The files.</response>
	[Authorize(Scopes.ReadCorpora)]
	[HttpGet("{id}/files")]
	public async Task<ActionResult<IEnumerable<DataFileDto>>> GetAllDataFilesAsync(string id)
	{
		Corpus? corpus = await _corpusService.GetAsync(id);
		if (corpus == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(corpus))
			return Forbid();

		return Ok(corpus.Files.Select(f => Map(id, f)));
	}

	/// <summary>
	/// Gets a data file for a corpus.
	/// </summary>
	/// <param name="id">The corpus id.</param>
	/// <param name="fileId">The data file id.</param>
	/// <response code="200">The data file.</response>
	[Authorize(Scopes.ReadCorpora)]
	[HttpGet("{id}/files/{fileId}")]
	public async Task<ActionResult<DataFileDto>> GetDataFileAsync(string id, string fileId)
	{
		Corpus? corpus = await _corpusService.GetAsync(id);
		if (corpus == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(corpus))
			return Forbid();

		DataFile? dataFile = corpus.Files.FirstOrDefault(f => f.Id == fileId);
		if (dataFile == null)
			return NotFound();

		return Ok(Map(id, dataFile));
	}

	/// <summary>
	/// Deletes a data file from a corpus.
	/// </summary>
	/// <param name="id">The corpus id.</param>
	/// <param name="fileId">The data file id.</param>
	/// <response code="200">The data file was deleted successfully.</response>
	[Authorize(Scopes.UpdateTranslationEngines)]
	[HttpDelete("{id}/files/{fileId}")]
	[ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
	public async Task<ActionResult> DeleteDataFileAsync(string id, string fileId)
	{
		Corpus? corpus = await _corpusService.GetAsync(id);
		if (corpus == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(corpus))
			return Forbid();

		if (!await _corpusService.DeleteDataFileAsync(id, fileId))
			return NotFound();

		return Ok();
	}

	private DataFileDto Map(string corpusId, DataFile file)
	{
		return _mapper.Map<DataFileDto>(file, opts => opts.Items["CorpusId"] = corpusId);
	}
}
