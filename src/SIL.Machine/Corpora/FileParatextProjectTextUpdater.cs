namespace SIL.Machine.Corpora
{
    public class FileParatextProjectTextUpdater : ParatextProjectTextUpdaterBase
    {
        public FileParatextProjectTextUpdater(string projectDir, ParatextProjectSettings parentSettings = null)
            : base(
                new FileParatextProjectFileHandler(projectDir),
                FileParatextProjectSettingsParser.Parse(projectDir, parentSettings)
            ) { }
    }
}
