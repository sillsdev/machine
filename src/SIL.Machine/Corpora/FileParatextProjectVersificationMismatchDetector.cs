namespace SIL.Machine.Corpora
{
    public class FileParatextProjectVersificationMismatchDetector : ParatextProjectVersificationMismatchDetector
    {
        public FileParatextProjectVersificationMismatchDetector(string projectDir)
            : base(new FileParatextProjectFileHandler(projectDir)) { }
    }
}
