using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectTextUpdater : ParatextProjectTextUpdaterBase
    {
        public ZipParatextProjectTextUpdater(ZipArchive archive, ParatextProjectSettings parentSettings = null)
            : base(
                new ZipParatextProjectFileHandler(archive),
                ZipParatextProjectSettingsParser.Parse(archive, parentSettings)
            ) { }
    }
}
