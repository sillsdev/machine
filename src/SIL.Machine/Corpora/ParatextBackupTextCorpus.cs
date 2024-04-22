using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ParatextBackupTextCorpus : ScriptureTextCorpus
    {
        public ParatextBackupTextCorpus(string fileName, bool includeMarkers = false, bool includeAllText = false)
        {
            using (ZipArchive archive = ZipFile.OpenRead(fileName))
            {
                var parser = new ZipParatextProjectSettingsParser(archive);
                ParatextProjectSettings settings = parser.Parse();

                Versification = settings.Versification;

                foreach (ZipArchiveEntry sfmEntry in archive.Entries)
                {
                    if (settings.IsBookFileName(sfmEntry.FullName, out string bookId))
                    {
                        AddText(
                            new UsfmZipText(
                                settings.Stylesheet,
                                settings.Encoding,
                                bookId,
                                fileName,
                                sfmEntry.FullName,
                                Versification,
                                includeMarkers,
                                includeAllText
                            )
                        );
                    }
                }
            }
        }
    }
}
