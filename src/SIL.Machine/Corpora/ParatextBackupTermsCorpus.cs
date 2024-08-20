using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ParatextBackupTermsCorpus : ParatextTermsCorpusBase
    {
        private readonly ZipArchive _archive;

        public ParatextBackupTermsCorpus(
            ZipArchive archive,
            IEnumerable<string> termCategories,
            bool preferTermsLocalization = false
        )
        {
            _archive = archive;
            AddTexts(new ZipParatextProjectSettingsParser(archive).Parse(), termCategories, preferTermsLocalization);
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
