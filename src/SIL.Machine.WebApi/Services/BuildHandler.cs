using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Services
{
	public class BuildHandler : IBuildHandler
	{
		public virtual Task OnStarted(BuildContext content)
		{
			return Task.CompletedTask;
		}

		public virtual Task OnCompleted(BuildContext context)
		{
			return Task.CompletedTask;
		}

		public virtual Task OnCanceled(BuildContext context)
		{
			return Task.CompletedTask;
		}

		public virtual Task OnFailed(BuildContext context)
		{
			return Task.CompletedTask;
		}
	}
}
