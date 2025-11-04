using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectTermsParser : ParatextProjectTermsParserBase
    {
        public ZipParatextProjectTermsParser(ZipArchive archive)
            : base(new ZipParatextProjectFileHandler(archive), ZipParatextProjectSettingsParser.Parse(archive)) { }
    }
}
