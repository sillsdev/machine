namespace SIL.Machine.WebApi.Controllers;

/// <summary>
/// Webhooks
/// </summary>
[Area("Admin")]
[Route("[area]/[controller]")]
[Produces("application/json")]
[TypeFilter(typeof(OperationCancelledExceptionFilter))]
public class HooksController : Controller
{
	private readonly IAuthorizationService _authService;
	private readonly IRepository<Webhook> _hooks;
	private readonly IMapper _mapper;

	public HooksController(IAuthorizationService authService, IRepository<Webhook> hooks, IMapper mapper)
	{
		_authService = authService;
		_hooks = hooks;
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
		return (await _hooks.GetAllAsync(h => h.Owner == User.Identity!.Name)).Select(_mapper.Map<WebhookDto>);
	}

	/// <summary>
	/// Gets the specified webhook.
	/// </summary>
	/// <param name="id">The webhook id.</param>
	/// <response code="200">The webhook.</response>
	[Authorize(Scopes.ReadHooks)]
	[HttpGet("{id}")]
	public async Task<ActionResult<WebhookDto>> GetAsync(string id)
	{
		Webhook? hook = await _hooks.GetAsync(id);
		if (hook == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(hook))
			return Forbid();

		return Ok(_mapper.Map<WebhookDto>(hook));
	}

	/// <summary>
	/// Creates a new webhook.
	/// </summary>
	/// <param name="hook">The new webhook properties.</param>
	/// <response code="201">The webhook was created successfully.</response>
	[Authorize(Scopes.CreateHooks)]
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<WebhookDto>> CreateAsync([FromBody] NewWebhookDto hook)
	{
		var newHook = new Webhook
		{
			Url = hook.Url,
			Secret = hook.Secret,
			Events = hook.Events.ToList(),
			Owner = User.Identity!.Name!
		};

		await _hooks.InsertAsync(newHook);
		WebhookDto dto = _mapper.Map<WebhookDto>(newHook);
		return Created(dto.Href, dto);
	}

	/// <summary>
	/// Deletes the specified webhook.
	/// </summary>
	/// <param name="id">The webhook id.</param>
	/// <response code="200">The webhook was successfully deleted.</response>
	[Authorize(Scopes.DeleteHooks)]
	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteAsync(string id)
	{
		Webhook? hook = await _hooks.GetAsync(id);
		if (hook == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(hook))
			return Forbid();

		if ((await _hooks.DeleteAsync(id)) == null)
			return NotFound();
		return Ok();
	}

	private async Task<bool> AuthorizeIsOwnerAsync(Webhook hook)
	{
		AuthorizationResult result = await _authService.AuthorizeAsync(User, hook, "IsOwner");
		return result.Succeeded;
	}
}
