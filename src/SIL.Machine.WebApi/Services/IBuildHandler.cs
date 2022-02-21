namespace SIL.Machine.WebApi.Services;

public record BuildContext(Engine Engine, Build Build);

public interface IBuildHandler
{
	Task OnStarted(BuildContext content);
	Task OnCompleted(BuildContext context);
	Task OnCanceled(BuildContext context);
	Task OnFailed(BuildContext context);
}
