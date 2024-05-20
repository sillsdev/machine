namespace SIL.Machine.AspNetCore.Services;

public class NmtPreprocessBuildJob : PreprocessBuildJob
{
    public NmtPreprocessBuildJob(
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        IDistributedReaderWriterLockFactory lockFactory,
        ILogger<NmtPreprocessBuildJob> logger,
        IBuildJobService buildJobService,
        ISharedFileService sharedFileService,
        ICorpusService corpusService,
        ILanguageTagService languageTagService
    )
        : base(platformService, engines, lockFactory, logger, buildJobService, sharedFileService, corpusService)
    {
        _languageTagService = languageTagService;
        PretranslationEnabled = true;
        EngineType = TranslationEngineType.Nmt;
    }

    private readonly ILanguageTagService _languageTagService;

    protected override string ResolveLanguageCode(string languageCode)
    {
        _languageTagService.ConvertToFlores200Code(languageCode, out string resolvedCode);
        return resolvedCode;
    }
}
