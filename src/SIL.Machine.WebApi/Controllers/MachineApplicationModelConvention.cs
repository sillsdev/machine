using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace SIL.Machine.WebApi.Controllers
{
	public class MachineApplicationModelConvention : IApplicationModelConvention
	{
		private readonly AttributeRouteModel _prefixRoute;
		private readonly AuthorizeFilter _authorizeFilter;

		public MachineApplicationModelConvention(string prefix, IEnumerable<string> authenticationSchemes)
		{
			_prefixRoute = new AttributeRouteModel { Template = prefix };

			var policyBuilder = new AuthorizationPolicyBuilder(authenticationSchemes.ToArray());
			_authorizeFilter = new AuthorizeFilter(policyBuilder.RequireAuthenticatedUser().Build());
		}

		public void Apply(ApplicationModel application)
		{
			foreach (ControllerModel controller in application.Controllers
				.Where(c => c.ControllerType.Namespace == "SIL.Machine.WebApi.Controllers"))
			{
				SelectorModel selector = controller.Selectors.First(sm => sm.AttributeRouteModel != null);
				selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(_prefixRoute,
					selector.AttributeRouteModel);
				controller.Filters.Add(_authorizeFilter);
			}
		}
	}
}
