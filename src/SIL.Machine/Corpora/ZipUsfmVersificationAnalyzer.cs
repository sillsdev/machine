using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipUsfmVersificationAnalyzer : UsfmVersificationAnalyzerBase
    {
        public ZipUsfmVersificationAnalyzer(ZipArchive archive, ParatextProjectSettings parentSettings = null)
            : base(
                new ZipParatextProjectFileHandler(archive),
                ZipParatextProjectSettingsParser.Parse(archive, parentSettings)
            ) { }
    }
}
