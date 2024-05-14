namespace SIL.Machine.AspNetCore.Services;

public class NmtPreprocessBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IDistributedReaderWriterLockFactory lockFactory,
    ILogger<NmtPreprocessBuildJob> logger,
    IBuildJobService buildJobService,
    ISharedFileService sharedFileService,
    ICorpusService corpusService,
    ILanguageTagService languageTagService
) : PreprocessBuildJob(platformService, engines, lockFactory, logger, buildJobService, sharedFileService, corpusService)
{
    private readonly ILanguageTagService _languageTagService = languageTagService;

    public override TranslationEngineType GetEngineType() => TranslationEngineType.Nmt;

    public override bool GetPretranslationEnabled() => true;

    protected override string ResolveLanguageCode(string languageCode)
    {
        _languageTagService.ConvertToFlores200Code(languageCode, out string resolvedCode);
        return resolvedCode;
    }
}
