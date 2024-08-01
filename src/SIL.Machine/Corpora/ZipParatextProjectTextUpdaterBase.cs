namespace SIL.Machine.Corpora
{
    public abstract class ZipParatextProjectTextUpdaterBase : ParatextProjectTextUpdaterBase
    {
        protected ZipParatextProjectTextUpdaterBase(ZipParatextProjectSettingsParserBase settingsParser)
            : base(settingsParser) { }

        protected ZipParatextProjectTextUpdaterBase(ParatextProjectSettings settings)
            : base(settings) { }
    }
}
