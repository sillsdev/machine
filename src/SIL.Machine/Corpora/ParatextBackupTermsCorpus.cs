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
            bool useTermGlosses = true
        )
        {
            using (var archive = ZipFile.OpenRead(fileName))
            {
                ParatextProjectSettings settings = new ZipParatextProjectSettingsParser(archive).Parse();
                IEnumerable<(string, IReadOnlyList<string>)> glosses = new ZipParatextProjectTermsParser(
                    archive,
                    settings
                ).Parse(termCategories, useTermGlosses);
                string textId =
                    $"{settings.BiblicalTermsListType}:{settings.BiblicalTermsProjectName}:{settings.BiblicalTermsFileName}";

                IText text = new MemoryText(
                    textId,
                    glosses.Select(kvp => new TextRow(textId, kvp.Item1) { Segment = kvp.Item2 })
                );
                AddText(text);
            }
        }
    }
}
