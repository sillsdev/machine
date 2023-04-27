namespace SIL.Machine.AspNetCore.Services;

public class BuildProgress : IProgress<ProgressStatus>
{
    private readonly IPlatformService _platformService;
    private readonly string _buildId;
    private ProgressStatus _prevStatus;

    private DateTime lastReportTime = DateTime.Now;

    private float throttleTimeSeconds = 1;

    public BuildProgress(IPlatformService platformService, string buildId)
    {
        _platformService = platformService;
        _buildId = buildId;
    }

    public void Report(ProgressStatus value)
    {
        if (_prevStatus.Equals(value))
            return;

        if (DateTime.Now < lastReportTime.AddSeconds(throttleTimeSeconds))
            return;

        lastReportTime = DateTime.Now;
        _platformService.UpdateBuildStatusAsync(_buildId, value);
        _prevStatus = value;
    }
}
