using SIL.Machine.Corpora;

namespace SIL.Machine.PunctuationAnalysis;

public class MemoryParatextProjectQuoteConventionDetector(
    IDictionary<string, string>? files,
    ParatextProjectSettings? settings
) : ParatextProjectQuoteConventionDetector(new MemoryParatextProjectFileHandler(files, settings)) { }
