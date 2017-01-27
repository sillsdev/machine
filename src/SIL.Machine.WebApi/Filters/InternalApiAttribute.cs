using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Filters
{
	public class InternalApiAttribute : TypeFilterAttribute
	{
		public InternalApiAttribute()
			: base(typeof(InternalApiAttributeImpl))
		{
		}

		private class InternalApiAttributeImpl : IActionFilter
		{
			private readonly HashSet<string> _whitelistAddresses;

			public InternalApiAttributeImpl(IOptions<SecurityOptions> securityOptions)
			{
				_whitelistAddresses = new HashSet<string>(securityOptions.Value.InternalApiWhitelist.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries));
			}

			public void OnActionExecuting(ActionExecutingContext context)
			{
				string ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
				if (ipAddress == "::1")
					ipAddress = "127.0.0.1";
				if (ipAddress == null || !_whitelistAddresses.Contains(ipAddress))
					context.Result = new StatusCodeResult(403);
			}

			public void OnActionExecuted(ActionExecutedContext context)
			{
			}
		}
	}
}
