namespace SIL.Machine.AspNetCore.Services;

// TODO: The Hangfire implementation of the NMT train stage is not complete, DO NOT USE
// see https://github.com/sillsdev/machine/issues/103
public class NmtTrainBuildJob : HangfireBuildJob
{
    private readonly ISharedFileService _sharedFileService;
    private readonly IOptionsMonitor<ClearMLOptions> _options;

    public NmtTrainBuildJob(
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        IDistributedReaderWriterLockFactory lockFactory,
        IBuildJobService buildJobService,
        ILogger<NmtTrainBuildJob> logger,
        ISharedFileService sharedFileService,
        IOptionsMonitor<ClearMLOptions> options
    )
        : base(platformService, engines, lockFactory, buildJobService, logger)
    {
        _sharedFileService = sharedFileService;
        _options = options;
    }

    protected override async Task DoWorkAsync(
        string engineId,
        string buildId,
        object? data,
        string? buildOptions,
        IDistributedReaderWriterLock @lock,
        CancellationToken cancellationToken
    )
    {
        TranslationEngine? engine = await Engines.GetAsync(
            e => e.EngineId == engineId && e.CurrentBuild != null && e.CurrentBuild.BuildId == buildId,
            cancellationToken
        );
        if (engine is null)
            throw new OperationCanceledException();

        try
        {
            Installer.LogMessage += Log;
            await Installer.SetupPython();
            await Installer.TryInstallPip();
            await PipInstallModuleAsync(
                "sil-machine[jobs,huggingface,sentencepiece]",
                cancellationToken: cancellationToken
            );
            await PipInstallModuleAsync(
                "torch",
                indexUrl: "https://download.pytorch.org/whl/cu117",
                cancellationToken: cancellationToken
            );
            await PipInstallModuleAsync("accelerate", cancellationToken: cancellationToken);

            PythonEngine.Initialize();

            using (Py.GIL())
            {
                PythonEngine.Exec(
                    "from machine.jobs.build_nmt_engine import run\n"
                        + "args = {\n"
                        + $"    'model_type': '{_options.CurrentValue.ModelType}',\n"
                        + $"    'engine_id': '{engineId}',\n"
                        + $"    'build_id': '{buildId}',\n"
                        + $"    'src_lang': '{ConvertLanguageTag(engine.SourceLanguage)}',\n"
                        + $"    'trg_lang': '{ConvertLanguageTag(engine.TargetLanguage)}',\n"
                        + $"    'shared_file_uri': '{_sharedFileService.GetBaseUri()}',\n"
                        + (buildOptions is not null ? $"    'build_options': '''{buildOptions}''',\n" : "")
                        // buildRevision + 1 because the build revision is incremented after the build job
                        // is finished successfully but the file should be saved with the new revision number
                        + (
                            engine.IsModelPersisted
                                ? $"    'save_model': '{engine.Id}_{engine.BuildRevision + 1}',\n"
                                : ""
                        )
                        + $"    'clearml': False\n"
                        + "}\n"
                        + "run(args)\n"
                );
            }
        }
        finally
        {
            Installer.LogMessage -= Log;
        }
    }

    private void Log(string message)
    {
        Logger.LogInformation(message);
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

    public async Task PipInstallModuleAsync(
        string module_name,
        string version = "",
        string indexUrl = "",
        bool force = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Python.Deployment.Installer.LogMessage += Log;
            if (!Installer.IsModuleInstalled(module_name) || force)
            {
                string text = Path.Combine(Python.Deployment.Installer.EmbeddedPythonHome, "Scripts", "pip");
                string text2 = (force ? " --force-reinstall" : "");
                if (version.Length > 0)
                {
                    version = "==" + version;
                }
                if (indexUrl.Length > 0)
                {
                    text2 += " --index-url " + indexUrl;
                }

                await Python.Deployment.Installer.RunCommand(
                    text + " install " + module_name + version + " " + text2,
                    cancellationToken
                );
            }
        }
        finally
        {
            Python.Deployment.Installer.LogMessage -= Log;
        }
    }
}
