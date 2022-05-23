namespace SIL.Machine.WebApi.Services;

public class BuildProgress : IProgress<ProgressStatus>
{
    private readonly IRepository<Build> _buildRepo;
    private readonly string _buildId;
    private ProgressStatus _prevStatus;

    public BuildProgress(IRepository<Build> buildRepo, string buildId)
    {
        _buildRepo = buildRepo;
        _buildId = buildId;
    }

    public void Report(ProgressStatus value)
    {
        if (_prevStatus.Equals(value))
            return;

        _buildRepo.UpdateAsync(
            b => b.Id == _buildId && b.State == BuildState.Active,
            u =>
            {
                u.Set(b => b.Step, value.Step);
                if (value.PercentCompleted is not null)
                {
                    u.Set(
                        b => b.PercentCompleted,
                        Math.Round(value.PercentCompleted.Value, 4, MidpointRounding.AwayFromZero)
                    );
                }
                u.Set(b => b.Message, value.Message);
            }
        );
        _prevStatus = value;
    }
}
