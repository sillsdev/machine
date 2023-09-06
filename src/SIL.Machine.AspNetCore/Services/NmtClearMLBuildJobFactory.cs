namespace SIL.Machine.AspNetCore.Services;

public class NmtClearMLBuildJobFactory : IClearMLBuildJobFactory
{
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
                + $"    'max_steps': {_options.CurrentValue.MaxSteps},\n"
                + $"    'shared_file_uri': '{_sharedFileService.GetBaseUri()}',\n"
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
                out LanguageSubtag languageSubtag,
                out ScriptSubtag scriptSubtag,
                out _,
                out _
            )
        )
            return languageTag;

        // Convert to NLLB language codes
        return $"{languageSubtag.Iso3Code}_{scriptSubtag.Code}";
    }
}
