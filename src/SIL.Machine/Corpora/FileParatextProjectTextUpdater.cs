namespace SIL.Machine.Corpora
{
    public class FileParatextProjectTextUpdater : ParatextProjectTextUpdaterBase
    {
        public FileParatextProjectTextUpdater(string projectDir)
            : base(
                new FileParatextProjectFileHandler(projectDir),
                new FileParatextProjectSettingsParser(projectDir).Parse()
            ) { }
    }
}
