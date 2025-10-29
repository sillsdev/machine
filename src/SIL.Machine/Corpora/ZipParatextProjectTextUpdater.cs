using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectTextUpdater : ParatextProjectTextUpdaterBase
    {
        public ZipParatextProjectTextUpdater(ZipArchive archive)
            : base(new ZipParatextProjectFileHandler(archive), new ZipParatextProjectSettingsParser(archive).Parse())
        { }
    }
}
