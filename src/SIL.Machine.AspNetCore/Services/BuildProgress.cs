namespace SIL.Machine.AspNetCore.Services;

public class BuildProgress(IPlatformService platformService, string buildId) : IProgress<ProgressStatus>
{
    private readonly IPlatformService _platformService = platformService;
    private readonly string _buildId = buildId;
    private ProgressStatus _prevStatus;

    private DateTime _lastReportTime = DateTime.Now;

    private const float ThrottleTimeSeconds = 1;

    public void Report(ProgressStatus value)
    {
        if (_prevStatus.Equals(value))
            return;

        if (DateTime.Now < _lastReportTime.AddSeconds(ThrottleTimeSeconds))
            return;

        _lastReportTime = DateTime.Now;
        _platformService.UpdateBuildStatusAsync(_buildId, value);
        _prevStatus = value;
    }
}
