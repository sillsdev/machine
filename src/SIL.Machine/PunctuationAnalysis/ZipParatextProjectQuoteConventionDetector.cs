using System.IO.Compression;
using SIL.Machine.Corpora;

namespace SIL.Machine.PunctuationAnalysis
{
    public class ZipParatextProjectQuoteConventionDetector : ParatextProjectQuoteConventionDetector
    {
        public ZipParatextProjectQuoteConventionDetector(
            ZipArchive archive,
            ParatextProjectSettings parentSettings = null
        )
            : base(
                new ZipParatextProjectFileHandler(archive),
                ZipParatextProjectSettingsParser.Parse(archive, parentSettings)
            ) { }
    }
}
