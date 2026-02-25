using System.IO.Compression;

namespace SIL.Machine.Corpora
{
    public class ParatextBackupTextCorpus : ScriptureTextCorpus
    {
        public ParatextBackupTextCorpus(
            string fileName,
            bool includeMarkers = false,
            bool includeAllText = false,
            string parentFileName = null
        )
        {
            ParatextProjectSettings parentSettings = null;
            if (parentFileName != null)
            {
                using (var archive = ZipFile.OpenRead(parentFileName))
                {
                    parentSettings = ZipParatextProjectSettingsParser.Parse(archive);
                }
            }

            using (ZipArchive archive = ZipFile.OpenRead(fileName))
            {
                var parser = new ZipParatextProjectSettingsParser(archive, parentSettings);
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
                            {
                                Project = settings.Name,
                            }
                        );
                    }
                }
            }
        }
    }
}
