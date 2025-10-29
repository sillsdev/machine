namespace SIL.Machine.Corpora;

public class MemoryParatextProjectTermsParser(IDictionary<string, string>? files, ParatextProjectSettings? settings)
    : ParatextProjectTermsParserBase(
        new MemoryParatextProjectFileHandler(files),
        settings ?? new MemoryParatextProjectFileHandler.DefaultParatextProjectSettings()
    ) { }
