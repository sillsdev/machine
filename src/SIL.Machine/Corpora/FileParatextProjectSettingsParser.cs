namespace SIL.Machine.Corpora
{
    public class FileParatextProjectSettingsParser : ParatextProjectSettingsParserBase
    {
        public FileParatextProjectSettingsParser(string projectDir, ParatextProjectSettings parentSettings = null)
            : base(new FileParatextProjectFileHandler(projectDir), parentSettings) { }

        public static ParatextProjectSettings Parse(string projectDir, ParatextProjectSettings parentSettings = null)
        {
            return new FileParatextProjectSettingsParser(projectDir, parentSettings).Parse();
        }
    }
}
