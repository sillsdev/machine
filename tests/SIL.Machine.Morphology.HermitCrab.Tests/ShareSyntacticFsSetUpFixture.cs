using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Allows the full HermitCrab suite to exercise the copy-on-write optimization's disabled path. Sharing
/// remains enabled by default; set HC_SHARE_SYNTACTIC_FS to "false" or "0" before running the suite to
/// disable it. The original Morpher construction default is restored after the suite.
/// </summary>
[SetUpFixture]
[NonParallelizable]
public class ShareSyntacticFsSetUpFixture
{
    private bool _savedDefault;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _savedDefault = Morpher.DefaultShareSyntacticFeatureStructs;
        string? envValue = Environment.GetEnvironmentVariable("HC_SHARE_SYNTACTIC_FS");
        if (string.Equals(envValue, "false", StringComparison.OrdinalIgnoreCase) || envValue == "0")
            Morpher.DefaultShareSyntacticFeatureStructs = false;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Morpher.DefaultShareSyntacticFeatureStructs = _savedDefault;
    }
}
