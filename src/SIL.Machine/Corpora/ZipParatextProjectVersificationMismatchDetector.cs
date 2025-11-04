using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectVersificationMismatchDetector : ParatextProjectVersificationMismatchDetectorBase
    {
        public ZipParatextProjectVersificationMismatchDetector(ZipArchive archive)
            : base(new ZipParatextProjectFileHandler(archive), ZipParatextProjectSettingsParser.Parse(archive))
        { }
    }
}
