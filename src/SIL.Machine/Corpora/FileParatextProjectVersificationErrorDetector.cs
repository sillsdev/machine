namespace SIL.Machine.Corpora
{
    public class FileParatextProjectVersificationErrorDetector : ParatextProjectVersificationErrorDetectorBase
    {
        public FileParatextProjectVersificationErrorDetector(string projectDir)
            : base(new FileParatextProjectFileHandler(projectDir), FileParatextProjectSettingsParser.Parse(projectDir))
        { }
    }
}
