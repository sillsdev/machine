using System.IO;
using System.IO.Compression;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectSettingsParser : ZipParatextProjectSettingsParserBase
    {
        private readonly ZipArchive _archive;

        public ZipParatextProjectSettingsParser(ZipArchive archive)
        {
            _archive = archive;
        }

        protected override bool Exists(string fileName)
        {
            return _archive.GetEntry(fileName) != null;
        }

        protected override string Find(string extension)
        {
            ZipArchiveEntry entry = _archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(extension));
            if (entry == null)
                return null;
            return entry.FullName;
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
