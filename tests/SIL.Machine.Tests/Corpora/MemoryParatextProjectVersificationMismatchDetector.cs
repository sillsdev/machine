namespace SIL.Machine.Corpora;

public class MemoryParatextProjectVersificationMismatchDetector(
    IDictionary<string, string>? files = null,
    ParatextProjectSettings? settings = null
)
    : ParatextProjectVersificationMismatchDetectorBase(
        new MemoryParatextProjectFileHandler(files),
        settings ?? new MemoryParatextProjectFileHandler.DefaultParatextProjectSettings()
    ) { }
