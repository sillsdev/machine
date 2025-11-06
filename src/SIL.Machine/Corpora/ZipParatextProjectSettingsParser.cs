using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectSettingsParser : ParatextProjectSettingsParserBase
    {
        public ZipParatextProjectSettingsParser(ZipArchive archive)
            : base(new ZipParatextProjectFileHandler(archive)) { }

        public static ParatextProjectSettings Parse(ZipArchive archive)
        {
            return new ZipParatextProjectSettingsParser(archive).Parse();
        }
    }
}
