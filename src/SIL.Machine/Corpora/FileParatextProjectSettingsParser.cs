namespace SIL.Machine.Corpora
{
    public class FileParatextProjectSettingsParser : ParatextProjectSettingsParserBase
    {
        public FileParatextProjectSettingsParser(string projectDir)
            : base(new FileParatextProjectFileHandler(projectDir)) { }
    }
}
