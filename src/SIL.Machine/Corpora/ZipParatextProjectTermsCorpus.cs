using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ZipParatextProjectTermsCorpus : ParatextProjectTermsCorpusBase
    {
        private readonly ZipArchive _archive;

        public ZipParatextProjectTermsCorpus(
            ZipArchive archive,
            IEnumerable<string> termCategories,
            bool preferTermsLocalization = false
        )
            : base(new ZipParatextProjectSettingsParser(archive).Parse())
        {
            _archive = archive;
            AddTexts(termCategories, preferTermsLocalization);
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
