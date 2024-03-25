using System.IO;

namespace SIL.Machine.Corpora
{
    public class ParatextTextCorpus : ScriptureTextCorpus
    {
        public ParatextTextCorpus(string projectDir, bool includeMarkers = false, bool includeAllText = false)
        {
            var parser = new FileParatextProjectSettingsParser(projectDir);
            ParatextProjectSettings settings = parser.Parse();

            Versification = settings.Versification;

            foreach (
                string sfmFileName in Directory.EnumerateFiles(
                    projectDir,
                    $"{settings.FileNamePrefix}*{settings.FileNameSuffix}"
                )
            )
            {
                AddText(
                    new UsfmFileText(
                        settings.Stylesheet,
                        settings.Encoding,
                        sfmFileName,
                        Versification,
                        includeMarkers,
                        includeAllText
                    )
                );
            }
        }
    }
}
