namespace SIL.Machine.WebApi.Controllers;

[Route("hooks")]
public class WebhooksController : ControllerBase
{
	private readonly IWebhookService _hookService;
	private readonly IMapper _mapper;

	public WebhooksController(IAuthorizationService authService, IWebhookService hookService, IMapper mapper)
		: base(authService)
	{
		_hookService = hookService;
		_mapper = mapper;
	}

	/// <summary>
	/// Gets all webhooks.
	/// </summary>
	/// <response code="200">The webhooks.</response>
	[Authorize(Scopes.ReadHooks)]
	[HttpGet]
	public async Task<IEnumerable<WebhookDto>> GetAllAsync()
	{
		return (await _hookService.GetAllAsync(User.Identity!.Name!)).Select(_mapper.Map<WebhookDto>);
	}

	/// <summary>
	/// Gets a webhook.
	/// </summary>
	/// <param name="id">The webhook id.</param>
	/// <response code="200">The webhook.</response>
	[Authorize(Scopes.ReadHooks)]
	[HttpGet("{id}")]
	public async Task<ActionResult<WebhookDto>> GetAsync(string id)
	{
		Webhook? hook = await _hookService.GetAsync(id);
		if (hook == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(hook))
			return Forbid();

		return Ok(_mapper.Map<WebhookDto>(hook));
	}

	/// <summary>
	/// Creates a new webhook.
	/// </summary>
	/// <param name="hookConfig">The webhook configuration.</param>
	/// <response code="201">The webhook was created successfully.</response>
	[Authorize(Scopes.CreateHooks)]
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<WebhookDto>> CreateAsync([FromBody] NewWebhookDto hookConfig)
	{
		var newHook = new Webhook
		{
			Url = hookConfig.Url,
			Secret = hookConfig.Secret,
			Events = hookConfig.Events.ToList(),
			Owner = User.Identity!.Name!
		};

		await _hookService.CreateAsync(newHook);
		WebhookDto dto = _mapper.Map<WebhookDto>(newHook);
		return Created(dto.Href, dto);
	}

	/// <summary>
	/// Deletes a webhook.
	/// </summary>
	/// <param name="id">The webhook id.</param>
	/// <response code="200">The webhook was successfully deleted.</response>
	[Authorize(Scopes.DeleteHooks)]
	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteAsync(string id)
	{
		Webhook? hook = await _hookService.GetAsync(id);
		if (hook == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(hook))
			return Forbid();

		if (!await _hookService.DeleteAsync(id))
			return NotFound();
		return Ok();
	}
}
