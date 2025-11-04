namespace SIL.Machine.Corpora
{
    public class FileParatextProjectSettingsParser : ParatextProjectSettingsParserBase
    {
        public FileParatextProjectSettingsParser(string projectDir)
            : base(new FileParatextProjectFileHandler(projectDir)) { }

        public static ParatextProjectSettings Parse(string projectDir)
        {
            return new FileParatextProjectSettingsParser(projectDir).Parse();
        }
    }
}
