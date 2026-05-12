namespace SIL.Machine.Corpora;

public class MemoryParatextProjectTextUpdater(
    IDictionary<string, string>? files = null,
    ParatextProjectSettings? settings = null
)
    : ParatextProjectTextUpdaterBase(
        new MemoryParatextProjectFileHandler(files),
        settings ?? new DefaultParatextProjectSettings()
    );
