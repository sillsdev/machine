namespace SIL.Machine.AspNetCore.Services;

public class NmtClearMLBuildJobFactory : IClearMLBuildJobFactory
{
    private readonly ISharedFileService _sharedFileService;
    private readonly ILanguageTagService _languageTagService;
    private readonly IRepository<TranslationEngine> _engines;
    private readonly IOptionsMonitor<ClearMLOptions> _options;

    public NmtClearMLBuildJobFactory(
        ISharedFileService sharedFileService,
        ILanguageTagService languageTagService,
        IRepository<TranslationEngine> engines,
        IOptionsMonitor<ClearMLOptions> options
    )
    {
        _sharedFileService = sharedFileService;
        _languageTagService = languageTagService;
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

            Uri sharedFileUri = _sharedFileService.GetBaseUri();
            string baseUri = sharedFileUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
            string folder = sharedFileUri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            return "from machine.jobs.build_nmt_engine import run\n"
                + "args = {\n"
                + $"    'model_type': '{_options.CurrentValue.ModelType}',\n"
                + $"    'engine_id': '{engineId}',\n"
                + $"    'build_id': '{buildId}',\n"
                + $"    'src_lang': '{_languageTagService.ConvertToFlores200Code(engine.SourceLanguage)}',\n"
                + $"    'trg_lang': '{_languageTagService.ConvertToFlores200Code(engine.TargetLanguage)}',\n"
                + $"    'shared_file_uri': '{baseUri}',\n"
                + $"    'shared_file_folder': '{folder}',\n"
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
}
