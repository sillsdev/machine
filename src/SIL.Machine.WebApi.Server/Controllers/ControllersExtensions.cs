using Microsoft.AspNetCore.Mvc;

namespace SIL.Machine.WebApi.Server.Controllers
{
	internal static class ControllersExtensions
	{
		public static string GetEntityUrl(this IUrlHelper urlHelper, string routeName, string id)
		{
			return urlHelper.RouteUrl(routeName) + $"/id:{id}";
		}
	}
}

