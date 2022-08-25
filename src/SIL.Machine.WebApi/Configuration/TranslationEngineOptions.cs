namespace SIL.Machine.WebApi.Configuration;

public class TranslationEngineOptions
{
    public const string Key = "TranslationEngine";

    public string EnginesDir { get; set; } = "translation_engines";
    public string ParentModelsDir { get; set; } = "parents";
    public TimeSpan EngineCommitFrequency { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan InactiveEngineTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public ISet<TranslationEngineType> Types { get; set; } =
        new HashSet<TranslationEngineType> { TranslationEngineType.Nmt, TranslationEngineType.SmtTransfer };
}
