namespace SIL.Machine.Corpora;

public class MemoryParatextProjectTextUpdater(IDictionary<string, string>? files, ParatextProjectSettings settings)
    : ParatextProjectTextUpdaterBase(new MemoryParatextProjectFileHandler(files), settings);
