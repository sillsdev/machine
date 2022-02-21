namespace SIL.Machine.WebApi.Services;

public class BuildProgress : IProgress<ProgressStatus>
{
	private readonly IBuildRepository _buildRepo;
	private readonly Build _build;

	public BuildProgress(IBuildRepository buildRepo, Build build)
	{
		_buildRepo = buildRepo;
		_build = build;
	}

	public void Report(ProgressStatus value)
	{
		if (_build.State != BuildStates.Active
			|| (_build.PercentCompleted == value.PercentCompleted && _build.Message == value.Message))
		{
			return;
		}

		_build.PercentCompleted = value.PercentCompleted;
		_build.Message = value.Message;
		_buildRepo.UpdateAsync(_build).WaitAndUnwrapException();
	}
}
