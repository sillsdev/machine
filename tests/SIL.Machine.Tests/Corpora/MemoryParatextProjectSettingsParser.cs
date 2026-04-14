namespace SIL.Machine.Corpora;

public class MemoryParatextProjectSettingsParser(
    IDictionary<string, string>? files = null,
    ParatextProjectSettings? parentSettings = null
) : ParatextProjectSettingsParserBase(new MemoryParatextProjectFileHandler(files), parentSettings);
