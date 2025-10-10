namespace SIL.Machine.Corpora;

public class MemoryParatextProjectVersificationMismatchDetector(
    IDictionary<string, string> files,
    ParatextProjectSettings settings
) : ParatextProjectVersificationMismatchDetector(new MemoryParatextProjectFileHandler(files, settings)) { }
