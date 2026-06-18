namespace SIL.Machine.Corpora;

public class MemoryUsfmVersificationAnalyzer(
    IDictionary<string, string>? files = null,
    ParatextProjectSettings? settings = null
)
    : UsfmVersificationAnalyzerBase(
        new MemoryParatextProjectFileHandler(files),
        settings ?? new DefaultParatextProjectSettings()
    );
