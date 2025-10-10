using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectVersificationMismatchDetector : ParatextProjectVersificationMismatchDetector
    {
        public ZipParatextProjectVersificationMismatchDetector(ZipArchive archive)
            : base(new ZipParatextProjectFileHandler(archive)) { }
    }
}
