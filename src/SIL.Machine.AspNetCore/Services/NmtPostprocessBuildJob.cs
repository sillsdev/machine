namespace SIL.Machine.AspNetCore.Services;

public class NmtPostprocessBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IDistributedReaderWriterLockFactory lockFactory,
    IBuildJobService buildJobService,
    ILogger<NmtPostprocessBuildJob> logger,
    ISharedFileService sharedFileService
) : PostprocessBuildJob(platformService, engines, lockFactory, buildJobService, logger, sharedFileService)
{
    public override bool GetPretranslationEnabled() => true;
}
