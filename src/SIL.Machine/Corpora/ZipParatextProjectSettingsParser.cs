using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectSettingsParser : ParatextProjectSettingsParserBase
    {
        public ZipParatextProjectSettingsParser(ZipArchive archive, ParatextProjectSettings parentSettings = null)
            : base(new ZipParatextProjectFileHandler(archive), parentSettings) { }

        public static ParatextProjectSettings Parse(ZipArchive archive, ParatextProjectSettings parentSettings = null)
        {
            return new ZipParatextProjectSettingsParser(archive, parentSettings).Parse();
        }
    }
}
