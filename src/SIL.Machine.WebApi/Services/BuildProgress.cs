namespace SIL.Machine.WebApi.Services;

public class BuildProgress : IProgress<ProgressStatus>
{
	private readonly IRepository<Build> _buildRepo;
	private readonly string _buildId;
	private Build? _build;

	public BuildProgress(IRepository<Build> buildRepo, string buildId)
	{
		_buildRepo = buildRepo;
		_buildId = buildId;
	}

	public void Report(ProgressStatus value)
	{
		if (_build is not null
			&& (_build.State != BuildState.Active
				|| (_build.PercentCompleted == value.PercentCompleted && _build.Message == value.Message)))
		{
			return;
		}

		_build = _buildRepo.UpdateAsync(_buildId, u => u
			.Set(b => b.PercentCompleted, Math.Round(value.PercentCompleted, 4, MidpointRounding.AwayFromZero))
			.Set(b => b.Message, value.Message)).WaitAndUnwrapException();
	}
}
