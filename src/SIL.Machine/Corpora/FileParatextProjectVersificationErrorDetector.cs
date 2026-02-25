namespace SIL.Machine.Corpora
{
    public class FileParatextProjectVersificationErrorDetector : ParatextProjectVersificationErrorDetectorBase
    {
        public FileParatextProjectVersificationErrorDetector(
            string projectDir,
            ParatextProjectSettings parentSettings = null
        )
            : base(
                new FileParatextProjectFileHandler(projectDir),
                FileParatextProjectSettingsParser.Parse(projectDir, parentSettings)
            ) { }
    }
}
