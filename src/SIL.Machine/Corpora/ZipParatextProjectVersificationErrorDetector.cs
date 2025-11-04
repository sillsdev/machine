using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectVersificationErrorDetector : ParatextProjectVersificationErrorDetectorBase
    {
        public ZipParatextProjectVersificationErrorDetector(ZipArchive archive)
            : base(new ZipParatextProjectFileHandler(archive), ZipParatextProjectSettingsParser.Parse(archive)) { }
    }
}
