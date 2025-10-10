using System.IO;
using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectFileHandler : IParatextProjectFileHandler
    {
        private readonly ZipArchive _archive;
        private readonly ParatextProjectSettings _settings;

        public ZipParatextProjectFileHandler(ZipArchive archive)
        {
            _archive = archive;
            _settings = new ZipParatextProjectSettingsParser(archive).Parse();
        }

        public bool Exists(string fileName)
        {
            return _archive.GetEntry(fileName) != null;
        }

        public Stream Open(string fileName)
        {
            ZipArchiveEntry entry = _archive.GetEntry(fileName);
            if (entry == null)
                return null;
            return entry.Open();
        }

        public ParatextProjectSettings GetSettings()
        {
            return _settings;
        }
    }
}
