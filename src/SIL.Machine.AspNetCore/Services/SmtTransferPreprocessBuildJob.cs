namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferPreprocessBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IDistributedReaderWriterLockFactory lockFactory,
    ILogger<SmtTransferPreprocessBuildJob> logger,
    IBuildJobService buildJobService,
    ISharedFileService sharedFileService,
    ICorpusService corpusService
) : PreprocessBuildJob(platformService, engines, lockFactory, logger, buildJobService, sharedFileService, corpusService)
{
    public override TranslationEngineType GetEngineType() => TranslationEngineType.SmtTransfer;

    public override bool GetPretranslationEnabled() => false;
}
