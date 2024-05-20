namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferPreprocessBuildJob : PreprocessBuildJob
{
    public SmtTransferPreprocessBuildJob(
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        IDistributedReaderWriterLockFactory lockFactory,
        ILogger<SmtTransferPreprocessBuildJob> logger,
        IBuildJobService buildJobService,
        ISharedFileService sharedFileService,
        ICorpusService corpusService
    )
        : base(platformService, engines, lockFactory, logger, buildJobService, sharedFileService, corpusService)
    {
        EngineType = TranslationEngineType.SmtTransfer;
        PretranslationEnabled = false;
    }
}
