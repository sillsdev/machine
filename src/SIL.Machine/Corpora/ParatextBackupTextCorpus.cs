using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora
{
    public class ParatextBackupTextCorpus : ScriptureTextCorpus
    {
        public ParatextBackupTextCorpus(string fileName, bool includeMarkers = false)
        {
            using (ZipArchive archive = ZipFile.OpenRead(fileName))
            {
                var parser = new ZipParatextProjectSettingsParser(archive);
                ParatextProjectSettings settings = parser.Parse();

                Versification = settings.Versification;

                var regex = new Regex(
                    $"^{Regex.Escape(settings.FileNamePrefix)}.*{Regex.Escape(settings.FileNameSuffix)}$"
                );

                foreach (ZipArchiveEntry sfmEntry in archive.Entries.Where(e => regex.IsMatch(e.FullName)))
                    AddText(
                        new UsfmZipText(
                            settings.Stylesheet,
                            settings.Encoding,
                            fileName,
                            sfmEntry.FullName,
                            Versification,
                            includeMarkers
                        )
                    );
            }
        }
    }
}
