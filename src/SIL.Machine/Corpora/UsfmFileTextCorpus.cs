using System;
using System.IO;
using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class UsfmFileTextCorpus : ScriptureTextCorpus
    {
        public UsfmFileTextCorpus(
            string stylesheetFileName,
            Encoding encoding,
            string projectPath,
            ScrVers versification = null,
            bool includeMarkers = false,
            string filePattern = "*.SFM",
            bool includeAllText = false
        )
        {
            Versification = versification ?? ScrVers.English;
            var stylesheet = new UsfmStylesheet(stylesheetFileName);
            foreach (string sfmFileName in Directory.EnumerateFiles(projectPath, filePattern))
            {
                string id = GetId(sfmFileName, encoding);
                if (id != null)
                {
                    AddText(
                        new UsfmFileText(
                            stylesheet,
                            encoding,
                            id,
                            sfmFileName,
                            Versification,
                            includeMarkers,
                            includeAllText
                        )
                    );
                }
            }
        }

        private static string GetId(string fileName, Encoding encoding)
        {
            using (var reader = new StreamReader(fileName, encoding))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("\\id ", StringComparison.InvariantCulture))
                    {
                        string id = line.Substring(4);
                        int index = id.IndexOf(" ", StringComparison.OrdinalIgnoreCase);
                        // If the id is longer than 3 characters, truncate it to 3 characters.
                        if ((index == -1 || index > 3) && id.Length >= 3)
                            index = 3;
                        if (index != -1)
                            id = id.Substring(0, index).ToUpperInvariant();
                        return id.Trim();
                    }
                }
            }
            return null;
        }
    }
}
