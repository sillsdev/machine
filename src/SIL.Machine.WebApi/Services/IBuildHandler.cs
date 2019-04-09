using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Services
{
	public interface IBuildHandler
	{
		Task OnStarted(BuildContext content);
		Task OnCompleted(BuildContext context);
		Task OnCanceled(BuildContext context);
		Task OnFailed(BuildContext context);
	}
}
