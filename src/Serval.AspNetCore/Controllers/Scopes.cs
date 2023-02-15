namespace Serval.AspNetCore.Controllers;

public static class Scopes
{
    public const string CreateTranslationEngines = "create:translation_engines";
    public const string ReadTranslationEngines = "read:translation_engines";
    public const string UpdateTranslationEngines = "update:translation_engines";
    public const string DeleteTranslationEngines = "delete:translation_engines";

    public const string CreateCorpora = "create:corpora";
    public const string ReadCorpora = "read:corpora";
    public const string UpdateCorpora = "update:corpora";
    public const string DeleteCorpora = "delete:corpora";

    public const string CreateHooks = "create:hooks";
    public const string ReadHooks = "read:hooks";
    public const string DeleteHooks = "delete:hooks";

    public static IEnumerable<string> All =>
        new[]
        {
            CreateTranslationEngines,
            ReadTranslationEngines,
            UpdateTranslationEngines,
            DeleteTranslationEngines,
            CreateCorpora,
            ReadCorpora,
            UpdateCorpora,
            DeleteCorpora,
            CreateHooks,
            ReadHooks,
            DeleteHooks
        };
}
