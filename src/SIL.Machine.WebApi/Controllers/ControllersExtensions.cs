using Microsoft.AspNetCore.Mvc;

namespace SIL.Machine.WebApi.Controllers
{
	internal static class ControllersExtensions
	{
		public static string GetEntityUrl(this IUrlHelper urlHelper, string routeName, string id)
		{
			return urlHelper.RouteUrl(routeName) + $"/id:{id}";
		}

		public static ResourceDto CreateLinkDto(this IUrlHelper urlHelper, string routeName, string id)
		{
			return new ResourceDto
			{
				Id = id,
				Href = urlHelper.GetEntityUrl(routeName, id)
			};
		}
	}
}

