using System.Collections.Generic;
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

        public UsfmFileTextCorpus(
            IReadOnlyDictionary<string, string> idToUsfmContent,
            UsfmStylesheet stylesheet,
            ScrVers versification,
            bool includeMarkers = false,
            bool includeAllText = false
        )
        {
            foreach (var pair in idToUsfmContent)
            {
                AddText(
                    new UsfmMemoryText(
                        stylesheet,
                        pair.Key,
                        pair.Value,
                        versification: Versification,
                        includeMarkers: includeMarkers,
                        includeAllText: includeAllText
                    )
                );
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
            return null;
        }
    }
}
