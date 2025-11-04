namespace SIL.Machine.Corpora;

public class MemoryParatextProjectVersificationErrorDetector(
    IDictionary<string, string>? files = null,
    ParatextProjectSettings? settings = null
)
    : ParatextProjectVersificationErrorDetectorBase(
        new MemoryParatextProjectFileHandler(files),
        settings ?? new MemoryParatextProjectFileHandler.DefaultParatextProjectSettings()
    ) { }
