namespace SIL.Machine.Corpora;

public class MemoryParatextProjectVersificationMismatchDetector(
    IDictionary<string, string>? files = null,
    ParatextProjectSettings? settings = null
) : ParatextProjectVersificationMismatchDetector(new MemoryParatextProjectFileHandler(files, settings)) { }
