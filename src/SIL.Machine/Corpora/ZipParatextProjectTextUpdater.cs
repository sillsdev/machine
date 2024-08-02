using System.IO;
using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectTextUpdater : ParatextProjectTextUpdaterBase
    {
        private readonly ZipArchive _archive;

        public ZipParatextProjectTextUpdater(ZipArchive archive)
            : base(new ZipParatextProjectSettingsParser(archive))
        {
            _archive = archive;
        }

        protected override bool Exists(string fileName)
        {
            return _archive.GetEntry(fileName) != null;
        }

        protected override Stream Open(string fileName)
        {
            ZipArchiveEntry entry = _archive.GetEntry(fileName);
            if (entry == null)
                return null;
            return entry.Open();
        }
    }
}
