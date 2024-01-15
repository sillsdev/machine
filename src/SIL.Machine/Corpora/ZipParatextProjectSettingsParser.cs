using System.IO;
using System.IO.Compression;
using System.Linq;
using SIL.IO;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectSettingsParser : ParatextProjectSettingsParserBase
    {
        private readonly ZipArchive _archive;

        public ZipParatextProjectSettingsParser(ZipArchive archive)
        {
            _archive = archive;
        }

        protected override UsfmStylesheet CreateStylesheet(string fileName)
        {
            TempFile stylesheetTempFile = null;
            TempFile customStylesheetTempFile = null;
            try
            {
                string stylesheetPath = fileName;
                ZipArchiveEntry stylesheetEntry = _archive.GetEntry(fileName);
                if (stylesheetEntry != null)
                {
                    stylesheetTempFile = TempFile.CreateAndGetPathButDontMakeTheFile();
                    stylesheetEntry.ExtractToFile(stylesheetTempFile.Path);
                    stylesheetPath = stylesheetTempFile.Path;
                }

                string customStylesheetPath = null;
                ZipArchiveEntry customStylesheetEntry = _archive.GetEntry("custom.sty");
                if (customStylesheetEntry != null)
                {
                    customStylesheetTempFile = TempFile.CreateAndGetPathButDontMakeTheFile();
                    customStylesheetEntry.ExtractToFile(customStylesheetTempFile.Path);
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
