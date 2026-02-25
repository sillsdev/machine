using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectVersificationErrorDetector : ParatextProjectVersificationErrorDetectorBase
    {
        public ZipParatextProjectVersificationErrorDetector(
            ZipArchive archive,
            ParatextProjectSettings parentSettings = null
        )
            : base(
                new ZipParatextProjectFileHandler(archive),
                ZipParatextProjectSettingsParser.Parse(archive, parentSettings)
            ) { }
    }
}
