namespace SIL.Machine.WebApi.Controllers;

[ApiController]
[Produces("application/json")]
[TypeFilter(typeof(OperationCancelledExceptionFilter))]
[TypeFilter(typeof(NotSupportedExceptionFilter))]
public class ControllerBase : Controller
{
	private readonly IAuthorizationService _authService;
	protected ControllerBase(IAuthorizationService authService)
	{
		_authService = authService;
	}

	protected async Task<bool> AuthorizeIsOwnerAsync(IOwnedEntity ownedEntity)
	{
		AuthorizationResult result = await _authService.AuthorizeAsync(User, ownedEntity, "IsOwner");
		return result.Succeeded;
	}
}
