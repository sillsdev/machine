using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class ParatextBackupTermsCorpus : DictionaryTextCorpus
    {
        public ParatextBackupTermsCorpus(
            string fileName,
            IEnumerable<string> termCategories,
            bool useTermGlosses = true,
            IDictionary<string, HashSet<int>> chapters = null
        )
        {
            using (var archive = ZipFile.OpenRead(fileName))
            {
                IEnumerable<(string, IReadOnlyList<string>)> glosses = new ZipParatextProjectTermsParser(archive)
                    .Parse(termCategories, useTermGlosses, chapters)
                    .OrderBy(g => g.TermId);

                ParatextProjectSettings settings = new ZipParatextProjectSettingsParser(archive).Parse();

                string textId =
                    $"{settings.BiblicalTermsListType}:{settings.BiblicalTermsProjectName}:{settings.BiblicalTermsFileName}";

                IText text = new MemoryText(
                    textId,
                    glosses.SelectMany(kvp =>
                        kvp.Item2.Select(gloss => new TextRow(textId, kvp.Item1) { Segment = new string[] { gloss } })
                    )
                );
                AddText(text);
            }
        }
    }
}
