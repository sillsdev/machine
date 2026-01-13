using System.IO;
using System.IO.Compression;
using System.Linq;
using SIL.IO;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectFileHandler : IParatextProjectFileHandler
    {
        private readonly ZipArchive _archive;

        public ZipParatextProjectFileHandler(ZipArchive archive)
        {
            _archive = archive;
        }

        public bool Exists(string fileName)
        {
            return _archive.Entries.Any(e =>
                e.FullName.Equals(fileName, System.StringComparison.InvariantCultureIgnoreCase)
            );
        }

        public Stream Open(string fileName)
        {
            ZipArchiveEntry entry = _archive.Entries.FirstOrDefault(e =>
                e.FullName.Equals(fileName, System.StringComparison.InvariantCultureIgnoreCase)
            );
            if (entry == null)
                return null;
            return entry.Open();
        }

        public string Find(string extension)
        {
            ZipArchiveEntry entry = _archive.Entries.FirstOrDefault(e =>
                e.FullName.EndsWith(extension, System.StringComparison.InvariantCultureIgnoreCase)
            );
            if (entry == null)
                return null;
            return entry.FullName;
        }

        public UsfmStylesheet CreateStylesheet(string fileName)
        {
            TempFile stylesheetTempFile = null;
            TempFile customStylesheetTempFile = null;
            try
            {
                string stylesheetPath = fileName;
                if (Exists(fileName))
                {
                    stylesheetTempFile = TempFile.CreateAndGetPathButDontMakeTheFile();
                    using (Stream source = Open(fileName))
                    using (Stream target = File.OpenWrite(stylesheetTempFile.Path))
                    {
                        source.CopyTo(target);
                    }
                    stylesheetPath = stylesheetTempFile.Path;
                }

                string customStylesheetPath = null;
                if (Exists("custom.sty"))
                {
                    customStylesheetTempFile = TempFile.CreateAndGetPathButDontMakeTheFile();
                    using (Stream source = Open("custom.sty"))
                    using (Stream target = File.OpenWrite(customStylesheetTempFile.Path))
                    {
                        source.CopyTo(target);
                    }
                    customStylesheetPath = customStylesheetTempFile.Path;
                }
                return new UsfmStylesheet(stylesheetPath, customStylesheetPath);
            }
            finally
            {
                stylesheetTempFile?.Dispose();
                customStylesheetTempFile?.Dispose();
            }
        }
    }
}
