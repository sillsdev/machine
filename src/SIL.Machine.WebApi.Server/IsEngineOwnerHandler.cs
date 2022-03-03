using Microsoft.AspNetCore.Authorization;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Server;

public class IsEngineOwnerHandler : AuthorizationHandler<IsOwnerRequirement, Engine>
{
	protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IsOwnerRequirement requirement,
		Engine resource)
	{
		if (context.User.Identity?.Name == resource.Owner)
			context.Succeed(requirement);
		return Task.CompletedTask;
	}
}
