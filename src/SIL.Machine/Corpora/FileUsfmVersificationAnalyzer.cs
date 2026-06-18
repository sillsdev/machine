namespace SIL.Machine.Corpora
{
    public class FileUsfmVersificationAnalyzer : UsfmVersificationAnalyzerBase
    {
        public FileUsfmVersificationAnalyzer(string projectDir, ParatextProjectSettings parentSettings = null)
            : base(
                new FileParatextProjectFileHandler(projectDir),
                FileParatextProjectSettingsParser.Parse(projectDir, parentSettings)
            ) { }
    }
}
