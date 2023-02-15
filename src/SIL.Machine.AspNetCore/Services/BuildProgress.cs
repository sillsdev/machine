namespace SIL.Machine.AspNetCore.Services;

public class BuildProgress : IProgress<ProgressStatus>
{
    private readonly IPlatformService _platformService;
    private readonly string _buildId;
    private ProgressStatus _prevStatus;

    public BuildProgress(IPlatformService platformService, string buildId)
    {
        _platformService = platformService;
        _buildId = buildId;
    }

    public void Report(ProgressStatus value)
    {
        if (_prevStatus.Equals(value))
            return;

        _platformService.UpdateBuildStatusAsync(_buildId, value);
        _prevStatus = value;
    }
}
