using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class UsfmZipText : UsfmTextBase
    {
        private readonly string _archiveFileName;
        private readonly string _path;

        public UsfmZipText(
            UsfmStylesheet stylesheet,
            Encoding encoding,
            string archiveFileName,
            string path,
            ScrVers versification = null,
            bool includeMarkers = false
        ) : base(GetId(archiveFileName, path, encoding), stylesheet, encoding, versification, includeMarkers)
        {
            _archiveFileName = archiveFileName;
            _path = path;
        }

        protected override IStreamContainer CreateStreamContainer()
        {
            return new ZipEntryStreamContainer(_archiveFileName, _path);
        }

        private static string GetId(string archiveFileName, string path, Encoding encoding)
        {
            using (var archive = ZipFile.OpenRead(archiveFileName))
            {
                ZipArchiveEntry entry = archive.GetEntry(path);
                using (var reader = new StreamReader(entry.Open(), encoding))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.StartsWith("\\id "))
                        {
                            string id = line.Substring(4);
                            int index = id.IndexOf(" ");
                            if (index != -1)
                                id = id.Substring(0, index);
                            return id.Trim();
                        }
                    }
                }
            }
            throw new InvalidOperationException("The USFM does not contain an 'id' marker.");
        }
    }
}
