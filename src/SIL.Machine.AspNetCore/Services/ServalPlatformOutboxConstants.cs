namespace SIL.Machine.AspNetCore.Services;

public static class ServalPlatformOutboxConstants
{
    public const string OutboxId = "ServalPlatform";

    public const string BuildStarted = "BuildStarted";
    public const string BuildCompleted = "BuildCompleted";
    public const string BuildCanceled = "BuildCanceled";
    public const string BuildFaulted = "BuildFaulted";
    public const string BuildRestarting = "BuildRestarting";
    public const string InsertPretranslations = "InsertPretranslations";
    public const string IncrementTranslationEngineCorpusSize = "IncrementTranslationEngineCorpusSize";
}
