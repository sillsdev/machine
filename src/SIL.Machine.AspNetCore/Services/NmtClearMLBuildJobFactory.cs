namespace SIL.Machine.AspNetCore.Services;

public class NmtClearMLBuildJobFactory : IClearMLBuildJobFactory
{
    private static readonly Dictionary<string, string> Macrolanguages = new Dictionary<string, string>
    {
        { "ar", "arb" },
        { "ms", "zsm" },
        { "lv", "lvs" },
        { "ne", "npi" },
        { "sw", "swh" }
    };

    private readonly ISharedFileService _sharedFileService;
    private readonly IRepository<TranslationEngine> _engines;
    private readonly IOptionsMonitor<ClearMLOptions> _options;

    public NmtClearMLBuildJobFactory(
        ISharedFileService sharedFileService,
        IRepository<TranslationEngine> engines,
        IOptionsMonitor<ClearMLOptions> options
    )
    {
        _sharedFileService = sharedFileService;
        _engines = engines;
        _options = options;
    }

    public TranslationEngineType EngineType => TranslationEngineType.Nmt;

    public async Task<string> CreateJobScriptAsync(
        string engineId,
        string buildId,
        string stage,
        object? data = null,
        string? buildOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        if (stage == NmtBuildStages.Train)
        {
            TranslationEngine? engine = await _engines.GetAsync(e => e.EngineId == engineId, cancellationToken);
            if (engine is null)
                throw new InvalidOperationException("The engine does not exist.");

            return "from machine.jobs.build_nmt_engine import run\n"
                + "args = {\n"
                + $"    'model_type': '{_options.CurrentValue.ModelType}',\n"
                + $"    'engine_id': '{engineId}',\n"
                + $"    'build_id': '{buildId}',\n"
                + $"    'src_lang': '{ConvertLanguageTag(engine.SourceLanguage)}',\n"
                + $"    'trg_lang': '{ConvertLanguageTag(engine.TargetLanguage)}',\n"
                + $"    'shared_file_uri': '{_sharedFileService.GetBaseUri()}',\n"
                + (buildOptions is not null ? $"    'build_options': '''{buildOptions}''',\n" : "")
                + $"    'clearml': True\n"
                + "}\n"
                + "run(args)\n";
        }
        else
        {
            throw new ArgumentException("Unknown build stage.", nameof(stage));
        }
    }

    private static string ConvertLanguageTag(string languageTag)
    {
        if (
            !IetfLanguageTag.TryGetSubtags(
                languageTag,
                out LanguageSubtag? languageSubtag,
                out ScriptSubtag? scriptSubtag,
                out _,
                out _
            )
        )
        {
            return languageTag;
        }

        // Normalize Mandarin Chinese subtag to Chinese subtag
        if (languageSubtag.Code == "cmn")
            languageSubtag = StandardSubtags.RegisteredLanguages["zh"];
        // Normalize macrolanguage subtag to the corresponding standard language subtag
        else if (Macrolanguages.TryGetValue(languageSubtag.Code, out string? standardLanguageCode))
            languageSubtag = StandardSubtags.RegisteredLanguages[standardLanguageCode];

        if (scriptSubtag is null)
        {
            // if Chinese is specified without a script/region, then default to Simplified Chinese
            if (languageSubtag.Code == "zh")
                return "zho_Hans";
            return languageTag;
        }

        // Convert to NLLB language codes
        return $"{languageSubtag.Iso3Code}_{scriptSubtag.Code}";
    }
}
