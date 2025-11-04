namespace SIL.Machine.Corpora
{
    public class FileParatextProjectVersificationMismatchDetector : ParatextProjectVersificationMismatchDetectorBase
    {
        public FileParatextProjectVersificationMismatchDetector(string projectDir)
            : base(new FileParatextProjectFileHandler(projectDir), FileParatextProjectSettingsParser.Parse(projectDir))
            { }
    }
}
