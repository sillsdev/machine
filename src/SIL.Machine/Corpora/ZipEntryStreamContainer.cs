using SIL.ObjectModel;
using System.IO.Compression;
using System.IO;

namespace SIL.Machine.Corpora
{
    public class ZipEntryStreamContainer : DisposableBase, IStreamContainer
    {
        private readonly ZipArchive _archive;
        private readonly ZipArchiveEntry _entry;

        public ZipEntryStreamContainer(string archiveFileName, string entryPath)
        {
            _archive = ZipFile.OpenRead(archiveFileName);
            _entry = _archive.GetEntry(entryPath);
        }

        public Stream OpenStream()
        {
            return _entry.Open();
        }

        protected override void DisposeManagedResources()
        {
            _archive.Dispose();
        }
    }
}
