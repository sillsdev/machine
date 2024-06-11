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

    protected override bool ResolveLanguageCodeForBaseModel(string languageCode, out string resolvedCode)
    {
        return _languageTagService.ConvertToFlores200Code(languageCode, out resolvedCode);
    }
}
