namespace SIL.Machine.Corpora
{
    public class FileParatextProjectVersificationMismatchDetector : ParatextProjectVersificationMismatchDetectorBase
    {
        public FileParatextProjectVersificationMismatchDetector(string projectDir)
            : base(
                new FileParatextProjectFileHandler(projectDir),
                new FileParatextProjectSettingsParser(projectDir).Parse()
            ) { }
    }
}
