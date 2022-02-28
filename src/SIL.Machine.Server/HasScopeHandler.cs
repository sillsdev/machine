using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.Server;

public class HasScopeHandler : IAuthorizationHandler
{
	public Task HandleAsync(AuthorizationHandlerContext context)
	{
		if (context.Resource is Engine engine)
		{
			Claim? scopeClaim = context.User.FindFirst(c => c.Type == "scope");
			if (context.User.Identity?.Name == engine.Owner && scopeClaim is not null)
			{
				HashSet<string> scopes = scopeClaim.Value.Split(' ').ToHashSet();
				foreach (OperationAuthorizationRequirement pendingRequirement in context.PendingRequirements)
				{
					string? requiredScope = null;
					switch (pendingRequirement.Name)
					{
						case nameof(Operations.Create):
							requiredScope = "create:engines";
							break;
						case nameof(Operations.Delete):
							requiredScope = "delete:engines";
							break;
						case nameof(Operations.Update):
							requiredScope = "update:engines";
							break;
						case nameof(Operations.Read):
							requiredScope = "read:engines";
							break;
					}
					if (requiredScope is not null && scopes.Contains(requiredScope))
						context.Succeed(pendingRequirement);
				}
			}
		}

		return Task.CompletedTask;
	}
}
