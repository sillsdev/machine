using System.IO;
using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipVersificationTable : VersificationTableBase
    {
        private readonly ZipArchive _archive;

        public ZipVersificationTable(ZipArchive archive)
        {
            _archive = archive;
        }

        protected override string ProjectName => null;

        protected override bool FileExists(string fileName)
        {
            return _archive.GetEntry(fileName) != null;
        }

        protected override Stream OpenFile(string fileName)
        {
            ZipArchiveEntry entry = _archive.GetEntry(fileName);
            if (entry == null)
                return null;
            return entry.Open();
        }
    }
}
