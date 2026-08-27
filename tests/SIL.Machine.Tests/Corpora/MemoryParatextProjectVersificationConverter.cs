namespace SIL.Machine.Corpora;

public class MemoryParatextProjectVersificationConverter(
    IDictionary<string, string>? files = null,
    ParatextProjectSettings? settings = null
)
    : ParatextProjectVersificationConverterBase(
        new MemoryParatextProjectFileHandler(files),
        settings ?? new DefaultParatextProjectSettings()
    );
